using System.Threading.Channels;
using A55.Subdivisions.Aws.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace A55.Subdivisions.Aws.Hosting.Job;

sealed class ConcurrentConsumerJob : IConsumerJob
{
    readonly IOptionsMonitor<SubConfig> config;
    readonly ConsumerFactory consumerFactory;
    readonly ILogger<ConcurrentConsumerJob> logger;
    readonly ISubdivisionsClient sub;

    public ConcurrentConsumerJob(
        ILogger<ConcurrentConsumerJob> logger,
        IOptionsMonitor<SubConfig> config,
        ISubdivisionsClient sub,
        ConsumerFactory consumerFactory
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
                Channel.CreateBounded<ConsumeRequest>(d.MaxConcurrency ?? config.CurrentValue.QueueMaxReceiveCount)
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
        var interval = describer.PollingInterval ?? TimeSpan.FromSeconds(config.CurrentValue.PollingIntervalInSeconds);
        using PeriodicTimer timer = new(interval);

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
                    await consumerFactory.ConsumeScoped(describer, message, ctx);
                }
                catch (Exception ex)
                {
                    logger.LogCritical(ex, "Subdivisions: Consumer Worker Failure");
                }
        }

        var tasks = Enumerable
            .Range(0, describer.MaxConcurrency ?? config.CurrentValue.QueueMaxReceiveCount)
            .Select(_ => TopicConsumer());
        await Task.WhenAll(tasks);
    }

    CancellationToken GetTimeoutToken(CancellationToken stoppingToken)
    {
        var timeoutToken =
            new CancellationTokenSource(TimeSpan.FromSeconds(config.CurrentValue.MessageTimeoutInSeconds));
        var combinedToken = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, timeoutToken.Token);
        return combinedToken.Token;
    }

    record struct ConsumeRequest(IMessage Message, CancellationToken Ctx);
}
