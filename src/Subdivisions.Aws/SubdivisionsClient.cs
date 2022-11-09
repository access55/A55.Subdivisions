using Subdivisions.Models;

namespace Subdivisions;

public record PublishResult(bool IsSuccess, Guid MessageId, Guid? CorrelationId);

public interface IProducerClient
{
    Task<PublishResult> Publish(
        string topicName,
        string message,
        Guid? correlationId = null,
        bool compressed = false,
        CancellationToken ctx = default);

    Task<PublishResult> Publish<T>(
        string topicName,
        T message,
        Guid? correlationId = null,
        bool compressed = false,
        CancellationToken ctx = default)
        where T : notnull;
}

public interface IConsumerClient
{
    ValueTask<IReadOnlyCollection<IMessage>> Receive(string topic, bool compressed = false,
        CancellationToken ctx = default);

    ValueTask<IReadOnlyCollection<IMessage<T>>> Receive<T>(string topic, bool compressed = false,
        CancellationToken ctx = default)
        where T : notnull;

    Task<IReadOnlyCollection<IMessage>> DeadLetters(string queueName, bool compressed = false,
        CancellationToken ctx = default);

    Task<IReadOnlyCollection<IMessage<T>>> DeadLetters<T>(string queueName, bool compressed = false,
        CancellationToken ctx = default)
        where T : notnull;
}

public interface ISubdivisionsClient : IProducerClient, IConsumerClient
{
}
