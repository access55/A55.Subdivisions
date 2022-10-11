using System.Collections.Immutable;
using A55.Subdivisions.Aws.Hosting.Job;
using A55.Subdivisions.Aws.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace A55.Subdivisions.Aws.Hosting;

class SubdivisionsHostedService : BackgroundService
{
    readonly ISubdivisionsBootstrapper bootstrapper;
    readonly ImmutableArray<IConsumerDescriber> describers;
    readonly IConsumerJob job;
    readonly ILogger<SubdivisionsHostedService> logger;

    public SubdivisionsHostedService(
        ILogger<SubdivisionsHostedService> logger,
        ISubdivisionsBootstrapper bootstrapper,
        IEnumerable<IConsumerDescriber> describers,
        IConsumerJob job)
    {
        this.logger = logger;
        this.bootstrapper = bootstrapper;
        this.job = job;
        this.describers = describers.ToImmutableArray();

        ValidateConsumerConfiguration();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Bootstrap(stoppingToken);
        await job.Start(describers, stoppingToken);
    }

    public Task Bootstrap(CancellationToken cancellationToken)
    {
        logger.LogDebug("Bootstrapping Subdivisions");
        var topicBootstrapper = describers
            .Select(async d => await bootstrapper
                .EnsureTopicExists(d.TopicName, cancellationToken));

        return Task.WhenAll(topicBootstrapper);
    }

    void ValidateConsumerConfiguration()
    {
        var duplicated = describers
            .GroupBy(d => d.TopicName)
            .Any(g => g.Count() > 1);

        if (duplicated)
            throw new SubdivisionsException("Duplicated topic definition");
    }
}
