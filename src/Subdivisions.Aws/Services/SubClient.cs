using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Subdivisions.Clients;
using Subdivisions.Models;

namespace Subdivisions.Services;

class AwsSubClient : ISubdivisionsClient
{
    readonly IOptions<SubConfig> config;
    readonly ILogger<AwsSubClient> logger;
    readonly ISubResourceManager resources;
    readonly ISubMessageSerializer serializer;

    readonly IProduceDriver producer;
    readonly IConsumeDriver consume;

    public AwsSubClient(
        ILogger<AwsSubClient> logger,
        IOptions<SubConfig> config,
        ISubMessageSerializer serializer,
        ISubResourceManager resources,
        IProduceDriver producer,
        IConsumeDriver consume
    )
    {
        this.logger = logger;
        this.config = config;
        this.serializer = serializer;
        this.resources = resources;
        this.producer = producer;
        this.consume = consume;
    }

    public ValueTask<IReadOnlyCollection<IMessage>> Receive(string topic, CancellationToken ctx = default) =>
        Receive(CreateTopicName(topic), ctx);

    public async ValueTask<IReadOnlyCollection<IMessage<T>>> Receive<T>(string topic, CancellationToken ctx = default)
        where T : notnull
    {
        var message = await Receive(topic, ctx);
        return message.Select(m => m.Map(s => serializer.Deserialize<T>(s))).ToArray();
    }

    public Task<IReadOnlyCollection<IMessage>> DeadLetters(string queueName, CancellationToken ctx = default)
    {
        var topic = CreateTopicName(queueName);
        return consume.ReceiveDeadLetters(topic, ctx);
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

    TopicId CreateTopicName(string name) => new(name, config.Value);

    internal async ValueTask<IReadOnlyCollection<IMessage>> Receive(TopicId topic, CancellationToken ctx) =>
        await consume.ReceiveMessages(topic, ctx);

    internal async Task<PublishResult> Publish(TopicId topic, string message, CancellationToken ctx)
    {
        logger.LogDebug("Publishing message on {Topic}", topic.TopicName);
        await resources.EnsureTopicExists(topic.Event, ctx);
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(topic);

        var publishResult = await producer.Produce(topic, message, ctx);

        return publishResult;
    }
}
