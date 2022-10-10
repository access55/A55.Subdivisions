﻿using A55.Subdivisions.Aws.Clients;
using A55.Subdivisions.Aws.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace A55.Subdivisions.Aws;

public interface IProducer
{
    Task<PublishResult> Publish(string topicName, string message, CancellationToken ctx = default);

    Task<PublishResult> Publish<T>(string topicName, T message, CancellationToken ctx = default)
        where T : notnull;
}

public interface IConsumerClient
{
    ValueTask<IReadOnlyCollection<IMessage>> Receive(string topic, CancellationToken ctx = default);

    ValueTask<IReadOnlyCollection<IMessage<T>>> Receive<T>(string topic, CancellationToken ctx = default)
        where T : notnull;

    Task<IReadOnlyCollection<IMessage>> DeadLetters(string queueName, CancellationToken ctx = default);

    Task<IReadOnlyCollection<IMessage<T>>> DeadLetters<T>(string queueName, CancellationToken ctx = default)
        where T : notnull;
}

public interface ISubdivisionsClient : IProducer, IConsumerClient
{
}

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

    public Task<PublishResult> Publish<T>(string topicName, T message, CancellationToken ctx = default)
        where T : notnull
    {
        var rawMessage = serializer.Serialize(message);
        return Publish(topicName, rawMessage, ctx);
    }

    public Task<PublishResult> Publish(string topicName, string message, CancellationToken ctx = default) =>
        Publish(CreateTopicName(topicName), message, ctx);

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
        return queue.ReceiveDeadLetters(topic.FullQueueName, ctx);
    }

    public async Task<IReadOnlyCollection<IMessage<T>>> DeadLetters<T>(string queueName,
        CancellationToken ctx = default)
        where T : notnull
    {
        var message = await DeadLetters(queueName, ctx);
        return message.Select(m => m.Map(s => serializer.Deserialize<T>(s))).ToArray();
    }

    TopicName CreateTopicName(string name) => new(
        name,
        config.Value
    );

    internal Task<PublishResult> Publish(TopicName topic, string message, CancellationToken ctx)
    {
        logger.LogDebug("Publishing message on {Topic}", topic.FullTopicName);
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(topic);

        AwsSqs.MessagePayload messagePayload = new(topic.Topic, clock.Now(), message);

        var payload = serializer.Serialize(messagePayload);
        return events.PushEvent(topic, payload, ctx);
    }

    internal async ValueTask<IReadOnlyCollection<IMessage>> Receive(TopicName topic, CancellationToken ctx) =>
        await queue.ReceiveMessages(topic.FullQueueName, ctx);
}
