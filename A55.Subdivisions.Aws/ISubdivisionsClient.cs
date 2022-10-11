namespace A55.Subdivisions.Aws;

public record PublishResult(bool IsSuccess, Guid MessageId);

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
