using A55.Subdivisions.Aws.Clients;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace A55.Subdivisions.Aws;

sealed class AwsSubClient : ISubdivisionsClient
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

    TopicName CreateTopicName(string name) => new(name, config.Value);

    public ValueTask<IReadOnlyCollection<IMessage>> Receive(string topic, CancellationToken ctx = default) =>
        Receive(CreateTopicName(topic), ctx);

    public async ValueTask<IReadOnlyCollection<IMessage<T>>> Receive<T>(string topic, CancellationToken ctx = default)
        where T : notnull
    {
        var message = await Receive(topic, ctx);
        return message.Select(m => m.Map(s => serializer.Deserialize<T>(s))).ToArray();
    }

    internal async ValueTask<IReadOnlyCollection<IMessage>> Receive(TopicName topic, CancellationToken ctx) =>
        await queue.ReceiveMessages(topic.FullQueueName, ctx);

    public Task<IReadOnlyCollection<IMessage>> DeadLetters(string queueName, CancellationToken ctx = default)
    {
        var topic = CreateTopicName(queueName);
        return queue.ReceiveDeadLetters(topic.FullQueueName, ctx);
    }

    public async Task<IReadOnlyCollection<IMessage<T>>> DeadLetters<T>(
        string queueName,
        CancellationToken ctx = default)
        where T : notnull
    {
        var message = await DeadLetters(queueName, ctx);
        return message.Select(m => m.Map(s => serializer.Deserialize<T>(s))).ToArray();
    }

    public Task<PublishResult> Publish<T>(string topicName, T message, CancellationToken ctx = default)
        where T : notnull
    {
        var rawMessage = serializer.Serialize(message);
        return Publish(topicName, rawMessage, ctx);
    }

    public Task<PublishResult> Publish(string topicName, string message, CancellationToken ctx = default) =>
        Publish(CreateTopicName(topicName), message, ctx);

    internal async Task<PublishResult> Publish(TopicName topic, string message, CancellationToken ctx)
    {
        logger.LogDebug("Publishing message on {Topic}", topic.FullTopicName);
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(topic);

        var messageId = Guid.NewGuid();
        AwsSqs.MessageEnvelope messagePayload = new(
            Event: topic.Topic, DateTime: clock.Now(), Payload: message,
            MessageId: messageId);

        var payload = serializer.Serialize(messagePayload);
        var publishResult = await events.PushEvent(topic, payload, ctx);

        return new(publishResult, messageId);
    }
}
