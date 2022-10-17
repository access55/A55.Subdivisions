using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Subdivisions.Clients;
using Subdivisions.Extensions;
using Subdivisions.Models;

namespace Subdivisions.Services;

public interface ISubResourceManager
{
    ValueTask EnsureQueueExists(string topic, CancellationToken ctx);
    ValueTask EnsureTopicExists(string topic, CancellationToken ctx);
    Task SetupLocalstack(CancellationToken ctx);
}

class AwsResourceManager : ISubResourceManager
{
    readonly SubConfig config;
    readonly AwsEvents events;
    readonly AwsKms kms;
    readonly ILogger<AwsResourceManager> logger;
    readonly AwsSns sns;
    readonly AwsSqs sqs;

    public AwsResourceManager(
        ILogger<AwsResourceManager> logger,
        IOptions<SubConfig> config,
        AwsEvents events,
        AwsSns sns,
        AwsSqs sqs,
        AwsKms kms
    )
    {
        this.config = config.Value;
        this.logger = logger;
        this.events = events;
        this.sns = sns;
        this.sqs = sqs;
        this.kms = kms;
    }

    public async ValueTask EnsureQueueExists(string topic, CancellationToken ctx)
    {
        TopicId topicId = new(topic, config);
        logger.LogInformation("Setting queue '{Queue}' up: Region={Region}", topicId.QueueName,
            config.RegionEndpoint().SystemName);

        if (await sqs.QueueExists(topicId.QueueName, ctx))
            return;

        var topicArn = await sns.EnsureTopic(topicId, ctx);
        var queueInfo = await sqs.CreateQueue(topicId.QueueName, ctx);
        await sns.Subscribe(topicArn, queueInfo.Arn, ctx);

        await WaitForQueue(topicId.QueueName, ctx).WaitAsync(TimeSpan.FromMinutes(3), ctx);
    }

    async Task WaitForQueue(string queueName, CancellationToken ctx)
    {
        while (await sqs.GetQueue(queueName, ctx) is null)
        {
            logger.LogInformation("Waiting queue be available...");
            await Task.Delay(1000, ctx);
            logger.LogInformation("Waiting available!");
        }
    }

    public async ValueTask EnsureTopicExists(string topic, CancellationToken ctx)
    {
        TopicId topicId = new(topic, config);
        if (await events.RuleExists(topicId, ctx))
            return;

        logger.LogDebug("Setting topic '{Topic}' up: Region={Region}", topicId.Event,
            config.RegionEndpoint().SystemName);

        if (!config.AutoCreateNewTopic)
            throw new InvalidOperationException(
                $"Topic '{topicId.TopicName}' for '{topicId.Event}' does not exists");

        await events.CreateRule(topicId, ctx);
        var topicArn = await sns.EnsureTopic(topicId, ctx);
        await events.PutTarget(topicId, topicArn, ctx);
    }

    public async Task SetupLocalstack(CancellationToken ctx)
    {
        var keyId = await kms.GetKey(ctx);
        if (keyId is null)
            await kms.CreteKey();
    }
}
