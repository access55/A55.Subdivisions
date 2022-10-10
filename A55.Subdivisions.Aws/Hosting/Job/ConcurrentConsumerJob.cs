using System.Collections.Immutable;
using System.Threading.Channels;
using A55.Subdivisions.Aws.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace A55.Subdivisions.Aws.Hosting.Job;

class ConcurrentConsumerJob : IConsumerJob
{
    readonly ConsumerFactory consumerFactory;
    readonly ILogger<ConcurrentConsumerJob> logger;
    readonly IOptionsMonitor<SubConfig> config;
    readonly ISubdivisionsClient sub;

    record struct ConsumeRequest(IMessage Message, CancellationToken Ctx);

    readonly ImmutableDictionary<string, Channel<ConsumeRequest>> channels;
    readonly ImmutableArray<IConsumerDescriber> describers;

    public ConcurrentConsumerJob(
        ILogger<ConcurrentConsumerJob> logger,
        IOptionsMonitor<SubConfig> config,
        ISubdivisionsClient sub,
        ConsumerFactory consumerFactory,
        IEnumerable<IConsumerDescriber> describers)
    {
        this.logger = logger;
        this.config = config;
        this.sub = sub;
        this.consumerFactory = consumerFactory;
        this.describers = describers.ToImmutableArray();

        channels = this.describers.ToImmutableDictionary(
            d => d.TopicName,
            d => Channel.CreateBounded<ConsumeRequest>(d.MaxConcurrency ?? config.CurrentValue.QueueMaxReceiveCount));
    }

    public async Task Start(CancellationToken stoppingToken)
    {
        var pollingWorkers = describers.Select(d => PollingWorker(d, stoppingToken));
        var consumerWorkers = describers.Select(d => ConsumerWorker(d, stoppingToken));
        await Task.WhenAll(pollingWorkers.Concat(consumerWorkers));
    }

    async Task PollingWorker(
        IConsumerDescriber describer,
        CancellationToken ctx)
    {
        var interval = describer.PollingInterval ?? TimeSpan.FromSeconds(config.CurrentValue.PollingIntervalInSeconds);
        using PeriodicTimer timer = new(interval);
        var channel = channels[describer.TopicName].Writer;

        while (await timer.WaitForNextTickAsync(ctx))
        {
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
                logger.LogCritical(exception: ex, message: "Subdivisions: Polling Worker Failure");
            }
        }

        channel.Complete();
    }

    async Task ConsumerWorker(IConsumerDescriber describer, CancellationToken stopToken)
    {
        var channel = channels[describer.TopicName].Reader;

        async Task TopicConsumer()
        {
            await foreach (var (message, ctx) in channel.ReadAllAsync(stopToken))
            {
                try
                {
                    await consumerFactory.ConsumeScoped(describer, message, ctx);
                }
                catch (Exception ex)
                {
                    logger.LogCritical(exception: ex, message: "Subdivisions: Consumer Worker Failure");
                }
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
}
