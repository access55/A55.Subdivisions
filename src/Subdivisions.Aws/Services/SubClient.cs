using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Subdivisions.Clients;
using Subdivisions.Hosting;
using Subdivisions.Models;

namespace Subdivisions.Services;

class AwsSubClient : ISubdivisionsClient
{
    readonly IOptions<SubConfig> config;
    readonly ILogger<AwsSubClient> logger;
    readonly ISubResourceManager resources;
    readonly ICorrelationResolver correlationResolver;
    readonly ISubMessageSerializer serializer;

    readonly IProduceDriver producer;
    readonly IConsumeDriver consume;

    public AwsSubClient(
        ILogger<AwsSubClient> logger,
        IOptions<SubConfig> config,
        ISubMessageSerializer serializer,
        ISubResourceManager resources,
        IProduceDriver producer,
        ICorrelationResolver correlationResolver,
        IConsumeDriver consume
    )
    {
        this.logger = logger;
        this.config = config;
        this.serializer = serializer;
        this.resources = resources;
        this.correlationResolver = correlationResolver;
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

    public Task<PublishResult> Publish<T>(string topicName, T message, Guid? correlationId = null,
        CancellationToken ctx = default)
        where T : notnull
    {
        var rawMessage = serializer.Serialize(message);
        return Publish(topicName, rawMessage, correlationId, ctx);
    }

    public Task<PublishResult> Publish(string topicName, string message, Guid? correlationId = null,
        CancellationToken ctx = default) =>
        Publish(CreateTopicName(topicName), message, correlationId, ctx);

    TopicId CreateTopicName(string name) => new(name, config.Value);

    internal async ValueTask<IReadOnlyCollection<IMessage>> Receive(TopicId topic, CancellationToken ctx) =>
        await consume.ReceiveMessages(topic, ctx);

    internal async Task<PublishResult> Publish(TopicId topic, string message, Guid? correlationId,
        CancellationToken ctx)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(topic);

        logger.LogDebug("Start send message on {Topic}", topic.TopicName);
        await resources.EnsureTopicExists(topic.Event, ctx);
        var validCorrelationId = correlationId ?? correlationResolver.GetId();
        var publishResult = await producer.Produce(topic, message, validCorrelationId, ctx);
        logger.LogInformation($"<- {topic.TopicName}[{publishResult.CorrelationId}.{publishResult.MessageId}]");
        logger.LogDebug("End send message on {Topic}", topic.TopicName);
        return publishResult;
    }
}
