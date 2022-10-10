using A55.Subdivisions.Aws.Hosting.Job;
using A55.Subdivisions.Aws.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace A55.Subdivisions.Aws.Hosting;

class SubdivisionsHostedService : BackgroundService
{
    readonly ISubdivisionsBootstrapper bootstrapper;
    readonly IConsumerJob job;
    readonly ILogger<SubdivisionsHostedService> logger;

    public SubdivisionsHostedService(
        ILogger<SubdivisionsHostedService> logger,
        ISubdivisionsBootstrapper bootstrapper,
        IConsumerJob job)
    {
        this.logger = logger;
        this.bootstrapper = bootstrapper;
        this.job = job;

        ValidateConsumerConfiguration();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Bootstrap(stoppingToken);
        await job.Start(stoppingToken);
    }

    public async Task Bootstrap(CancellationToken cancellationToken)
    {
        logger.LogDebug("Bootstrapping Subdivisions");
        var topicBootstrapper = describers
            .Select(d => bootstrapper
                .EnsureTopicExists(d.TopicName, cancellationToken)
                .AsTask());

        await Task.WhenAll(topicBootstrapper);
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
