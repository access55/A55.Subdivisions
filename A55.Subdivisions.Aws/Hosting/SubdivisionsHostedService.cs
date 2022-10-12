using System.Collections.Immutable;
using A55.Subdivisions.Aws.Hosting.Job;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace A55.Subdivisions.Aws.Hosting;

class SubdivisionsHostedService : BackgroundService
{
    readonly ISubResourceManager bootstrapper;
    readonly ImmutableArray<IConsumerDescriber> consumers;
    readonly IConsumerJob job;
    readonly ILogger<SubdivisionsHostedService> logger;

    public SubdivisionsHostedService(
        ILogger<SubdivisionsHostedService> logger,
        ISubResourceManager bootstrapper,
        IEnumerable<IConsumerDescriber> consumers,
        IConsumerJob job)
    {
        this.logger = logger;
        this.bootstrapper = bootstrapper;
        this.job = job;
        this.consumers = consumers.ToImmutableArray();

        ValidateConsumerConfiguration();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Bootstrap(stoppingToken);
        await job.Start(consumers, stoppingToken);
    }

    public async Task Bootstrap(CancellationToken cancellationToken)
    {
        logger.LogDebug("Bootstrapping Subdivisions");
        await Task.WhenAll(this.consumers
            .Select(async d =>
            {
                await bootstrapper.EnsureTopicExists(d.TopicName, cancellationToken);
                await bootstrapper.EnsureQueueExists(d.TopicName, cancellationToken);
            }));
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
