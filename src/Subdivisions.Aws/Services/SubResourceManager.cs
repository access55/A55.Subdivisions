using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Subdivisions.Clients;
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
        TopicName topicName = new(topic, config);
        logger.LogDebug("Setting queue '{Queue}' up: Region={Region}", topicName.FullQueueName,
            config.Endpoint.SystemName);

        if (await sqs.QueueExists(topicName.FullQueueName, ctx))
            return;

        var topicArn = await sns.EnsureTopic(topicName, ctx);
        var queueInfo = await sqs.CreateQueue(topicName.FullQueueName, ctx);
        await sns.Subscribe(topicArn, queueInfo.Arn, ctx);
    }

    public async ValueTask EnsureTopicExists(string topic, CancellationToken ctx)
    {
        TopicName topicName = new(topic, config);
        if (await events.RuleExists(topicName, ctx))
            return;

        logger.LogDebug("Setting topic '{Topic}' up: Region={Region}", topicName.Topic,
            config.Endpoint.SystemName);

        if (!config.AutoCreateNewTopic)
            throw new InvalidOperationException(
                $"Topic '{topicName.FullTopicName}' for '{topicName.Topic}' does not exists");

        await events.CreateRule(topicName, ctx);
        var topicArn = await sns.EnsureTopic(topicName, ctx);
        await events.PutTarget(topicName, topicArn, ctx);
    }

    public async Task SetupLocalstack(CancellationToken ctx)
    {
        var keyId = await kms.GetKey(ctx);
        if (keyId is null)
            await kms.CreteKey();
    }
}
