using System.Collections.Immutable;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Subdivisions.Hosting.Job;
using Subdivisions.Models;
using Subdivisions.Services;

namespace Subdivisions.Hosting;

class SubdivisionsHostedService : BackgroundService
{
    readonly ISubResourceManager bootstrapper;
    readonly SubConfig config;
    readonly ImmutableArray<IConsumerDescriber> consumers;
    readonly IConsumerJob job;
    readonly ILogger<SubdivisionsHostedService> logger;

    public SubdivisionsHostedService(
        ILogger<SubdivisionsHostedService> logger,
        ISubResourceManager bootstrapper,
        IEnumerable<IConsumerDescriber> consumers,
        IOptions<SubConfig> config,
        IConsumerJob job)
    {
        this.logger = logger;
        this.bootstrapper = bootstrapper;
        this.config = config.Value;
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

        if (config.Localstack)
            await bootstrapper.SetupLocalstack(cancellationToken);

        await Task.WhenAll(consumers
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
