using System.Threading.Channels;
using A55.Subdivisions.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace A55.Subdivisions.Hosting.Job;

sealed class ConcurrentConsumerJob : IConsumerJob
{
    readonly IOptionsMonitor<SubConfig> config;
    readonly IConsumerFactory consumerFactory;
    readonly ILogger<ConcurrentConsumerJob> logger;
    readonly ISubdivisionsClient sub;

    public ConcurrentConsumerJob(
        ILogger<ConcurrentConsumerJob> logger,
        IOptionsMonitor<SubConfig> config,
        ISubdivisionsClient sub,
        IConsumerFactory consumerFactory
    )
    {
        this.logger = logger;
        this.config = config;
        this.sub = sub;
        this.consumerFactory = consumerFactory;
    }

    public async Task Start(IReadOnlyCollection<IConsumerDescriber> describers, CancellationToken stoppingToken)
    {
        var workers =
            from d in describers
            let channel =
                Channel.CreateBounded<ConsumeRequest>(d.MaxConcurrency)
            from worker in new[]
            {
                PollingWorker(d, channel.Writer, stoppingToken), ConsumerWorker(d, channel.Reader, stoppingToken)
            }
            select worker;

        await Task.WhenAll(workers);
    }

    async Task PollingWorker(
        IConsumerDescriber describer,
        ChannelWriter<ConsumeRequest> channel,
        CancellationToken ctx)
    {
        using PeriodicTimer timer = new(describer.PollingInterval);

        while (await timer.WaitForNextTickAsync(ctx))
            try
            {
                await channel.WaitToWriteAsync(ctx);
                var timeoutToken = GetTimeoutToken(ctx);
                var messages = await sub.Receive(describer.TopicName, ctx);
                var tasks = messages.Select(async m =>
                    await channel.WriteAsync(new(m, timeoutToken), ctx));
                await Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, "Subdivisions: Polling Worker Failure");
            }

        channel.Complete();
    }

    async Task ConsumerWorker(
        IConsumerDescriber describer,
        ChannelReader<ConsumeRequest> channel,
        CancellationToken stopToken)
    {
        async Task TopicConsumer()
        {
            await foreach (var (message, ctx) in channel.ReadAllAsync(stopToken))
                try
                {
                    ctx.ThrowIfCancellationRequested();
                    await consumerFactory.ConsumeScoped(describer, message, ctx);
                }
                catch (Exception ex)
                {
                    logger.LogCritical(ex, "Subdivisions: Consumer Worker Failure");

                    if (describer.ErrorHandler is not null)
                        await describer.ErrorHandler(ex);
                }
        }

        var tasks = Enumerable
            .Range(0, describer.MaxConcurrency)
            .Select(_ => Task.Run(TopicConsumer, stopToken));
        await Task.WhenAll(tasks);
    }

    CancellationToken GetTimeoutToken(CancellationToken stoppingToken)
    {
        var timeoutToken = new CancellationTokenSource();
        var timeoutSeconds = config.CurrentValue.MessageTimeoutInSeconds;
        timeoutToken.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));
        var combinedToken = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, timeoutToken.Token);
        return combinedToken.Token;
    }

    record struct ConsumeRequest(IMessage<string> Message, CancellationToken Ctx);
}
