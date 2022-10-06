using A55.Subdivisions.Aws.Adapters;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace A55.Subdivisions.Aws;

class AwsSubdivisionsBootstrapper
{
    readonly SubConfig config;
    readonly ILogger<AwsSubdivisionsBootstrapper> logger;
    readonly AwsEvents events;
    readonly AwsSns sns;
    readonly AwsSqs sqs;

    public AwsSubdivisionsBootstrapper(
        ILogger<AwsSubdivisionsBootstrapper> logger,
        IOptions<SubConfig> config,
        AwsEvents events,
        AwsSns sns,
        AwsSqs sqs
    )
    {
        this.config = config.Value;
        this.logger = logger;
        this.events = events;
        this.sns = sns;
        this.sqs = sqs;
    }

    public async ValueTask EnsureTopicExists(string topic, CancellationToken ctx = default)
    {
        logger.LogInformation("Subdivisions Bootstrap");

        TopicName topicName = new TopicName(
            topic: topic,
            prefix: config.Prefix,
            sufix: config.Sufix,
            source: config.Source
        );

        if (!await events.RuleExists(topicName, ctx))
        {
            if (!config.AutoCreateNewTopic)
                throw new InvalidOperationException($"Topic '{topicName.FullTopicName}' for '{topic}' does not exists");

            await events.CreateRule(topicName, ctx);
            var topicArn = await sns.CreateTopic(topicName, ctx);
            var queueInfo = await sqs.CreateQueue(topicName.FullQueueName, ctx);
            await events.PutTarget(topicName.FullTopicName, topicArn, ctx);
            await sns.Subscribe(topicArn, queueInfo.Arn, ctx);
        }
        else if (!await sqs.QueueExists(topicName.FullQueueName, ctx))
        {
            var topicArn = await sns.CreateTopic(topicName, ctx);
            var queueInfo = await sqs.CreateQueue(topicName.FullQueueName, ctx);
            await sns.Subscribe(topicArn, queueInfo.Arn, ctx);
        }
    }
}
