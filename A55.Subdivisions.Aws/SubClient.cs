using A55.Subdivisions.Aws.Adapters;
using A55.Subdivisions.Aws.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace A55.Subdivisions.Aws;

interface ISubClient
{
    Task<PublishResult> Publish(string topicName, string message, CancellationToken ctx = default);
    ValueTask<IReadOnlyCollection<Message>> GetMessages(string topic, CancellationToken ctx = default);
}

sealed class AwsSubClient : ISubClient
{
    readonly ISubClock clock;
    readonly IOptions<SubConfig> config;
    readonly AwsEvents events;
    readonly ILogger<AwsSubClient> logger;
    readonly AwsSqs queue;
    readonly ISubMessageSerializer serializer;

    public AwsSubClient(
        ILogger<AwsSubClient> logger,
        IOptions<SubConfig> config,
        ISubClock clock,
        ISubMessageSerializer serializer,
        AwsEvents events,
        AwsSqs queue
    )
    {
        this.logger = logger;
        this.config = config;
        this.clock = clock;
        this.serializer = serializer;
        this.events = events;
        this.queue = queue;
    }

    public async Task<PublishResult> Publish(string topicName, string message, CancellationToken ctx = default) =>
        await Publish(GetTopic(topicName), message, ctx);

    public async ValueTask<IReadOnlyCollection<Message>> GetMessages(string topic, CancellationToken ctx = default) =>
        await GetMessages(GetTopic(topic), ctx);

    TopicName GetTopic(string name) => new(
        name,
        config.Value
    );

    internal async Task<PublishResult> Publish(TopicName topic, string message, CancellationToken ctx)
    {
        logger.LogDebug("Publishing message on {Topic}", topic.FullTopicName);
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(topic);

        MessagePayload messagePayload = new(topic.Topic, clock.Now(), message);

        var payload = serializer.Serialize(messagePayload);
        return await events.PushEvent(topic, payload, ctx);
    }

    internal async ValueTask<IReadOnlyCollection<Message>> GetMessages(TopicName topic, CancellationToken ctx) =>
        await queue.ReceiveMessages(topic.FullQueueName, ctx);
}
