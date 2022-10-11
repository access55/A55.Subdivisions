using System.Collections.Immutable;
using A55.Subdivisions.Aws.Hosting.Job;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace A55.Subdivisions.Aws.Hosting;

class SubdivisionsHostedService : BackgroundService
{
    readonly ISubResourceManager bootstrapper;
    readonly ImmutableArray<IConsumerDescriber> consumers;
    readonly ImmutableArray<IProducerDescriber> producers;
    readonly IConsumerJob job;
    readonly ILogger<SubdivisionsHostedService> logger;

    public SubdivisionsHostedService(
        ILogger<SubdivisionsHostedService> logger,
        ISubResourceManager bootstrapper,
        IEnumerable<IConsumerDescriber> consumers,
        IEnumerable<IProducerDescriber> producers,
        IConsumerJob job)
    {
        this.logger = logger;
        this.bootstrapper = bootstrapper;
        this.job = job;
        this.consumers = consumers.ToImmutableArray();
        this.producers = producers.ToImmutableArray();

        ValidateConsumerConfiguration();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Bootstrap(stoppingToken);
        await job.Start(consumers, stoppingToken);
    }

    public async Task Bootstrap(CancellationToken cancellationToken)
    {
        logger.LogDebug("Bootstrapping Subdivisions Producers");
        await Task.WhenAll(this.consumers
            .Select(async d => await bootstrapper
                .EnsureTopicExists(d.TopicName, cancellationToken)));

        logger.LogDebug("Bootstrapping Subdivisions Consumers");
        await Task.WhenAll(this.producers
            .Select(async d => await bootstrapper
                .EnsureQueueExists(d.TopicName, cancellationToken)));
    }

    void ValidateConsumerConfiguration()
    {
        var duplicated = consumers
            .GroupBy(d => d.TopicName)
            .Any(g => g.Count() > 1);

        if (duplicated)
            throw new SubdivisionsException("Duplicated topic definition");
    }
}
