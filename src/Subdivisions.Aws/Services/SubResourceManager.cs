using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Subdivisions.Clients;
using Subdivisions.Extensions;
using Subdivisions.Models;

namespace Subdivisions.Services;

interface ISubResourceManager
{
    ValueTask EnsureQueueExists(string topic, TopicNameOverride? nameOverride,
        CancellationToken ctx);

    ValueTask EnsureTopicExists(string topic, TopicNameOverride? nameOverride,
        CancellationToken ctx);

    ValueTask EnsureTopicExists(TopicId topic, CancellationToken ctx);

    ValueTask UpdateQueueAttr(string topic, TimeSpan? newTimeout, TopicNameOverride? nameOverride,
        CancellationToken ctx);

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

    public async ValueTask EnsureQueueExists(string topic,
        TopicNameOverride? nameOverride,
        CancellationToken ctx)
    {
        TopicId topicId = new(topic, config.FromOverride(nameOverride));

        if (nameOverride?.HasValues() == true)
        {
            TopicId original = new(topic, config);
            logger.LogInformation(
                $"Overriding queue name from '{original.QueueName}' to '{topicId.QueueName}'");
        }

        logger.LogInformation("Setting queue '{Queue}' up: Region={Region}",
            topicId.QueueName,
            config.RegionEndpoint().SystemName);

        if (await sqs.QueueExists(topicId.QueueName, ctx))
            return;

        var topicArn = await sns.EnsureTopic(topicId, ctx);
        var queueInfo = await sqs.CreateQueue(topicId.QueueName, ctx);
        logger.LogInformation(
            $"Subscribing {topicId.QueueName}[{queueInfo.Arn}] on {topicId.TopicName}[{topicArn}]");
        await sns.Subscribe(topicArn, queueInfo.Arn, ctx);

        await WaitForQueue(topicId.QueueName, ctx)
            .WaitAsync(TimeSpan.FromMinutes(5), ctx);
    }

    public async ValueTask UpdateQueueAttr(
        string topic,
        TimeSpan? newTimeout,
        TopicNameOverride? nameOverride,
        CancellationToken ctx)
    {
        TopicId topicId = new(topic, config.FromOverride(nameOverride));

        if (newTimeout is null)
            return;
        await sqs.UpdateQueueAttributes(topicId.QueueName, newTimeout.Value, ctx);
    }

    async Task WaitForQueue(string queueName, CancellationToken ctx)
    {
        while (await sqs.GetQueue(queueName, ctx) is null)
        {
            logger.LogInformation("Waiting queue be available...");
            await Task.Delay(TimeSpan.FromSeconds(2), ctx);
            logger.LogInformation("Not available yet.");
        }

        logger.LogInformation("Queue available!");
    }

    public async ValueTask EnsureTopicExists(string topic,
        TopicNameOverride? nameOverride,
        CancellationToken ctx)
    {
        var overrideConfig = config.FromOverride(nameOverride);
        TopicId topicId = new(topic, overrideConfig);

        if (nameOverride?.HasValues() == true)
        {
            TopicId original = new(topic, config);
            logger.LogInformation(
                $"Overriding topic name from '{original.TopicName}' to '{topicId.TopicName}'");
        }

        await EnsureTopicExists(topicId, ctx);
    }

    public async ValueTask EnsureTopicExists(TopicId topic,
        CancellationToken ctx)
    {
        if (await events.RuleExists(topic, ctx))
        {
            logger.LogInformation($"Rule {topic.TopicName} already exists");
            return;
        }

        logger.LogInformation("Setting topic '{Topic}' up: Region={Region}", topic.Event,
            config.RegionEndpoint().SystemName);

        if (!config.AutoCreateNewTopic)
            throw new InvalidOperationException(
                $"Topic '{topic.TopicName}' for '{topic.Event}' does not exists");

        await events.CreateRule(topic, ctx);
        var topicArn = await sns.EnsureTopic(topic, ctx);
        await events.PutTarget(topic, topicArn, ctx);
    }

    public async Task SetupLocalstack(CancellationToken ctx)
    {
        var keyId = await kms.GetKey(ctx);
        if (keyId is null)
            await kms.CreteKey();
    }
}
