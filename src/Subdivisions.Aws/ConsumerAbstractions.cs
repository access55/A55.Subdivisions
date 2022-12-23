namespace Subdivisions;

public sealed class MessageMeta
{
    public required Guid MessageId { get; init; }
    public required string Queue { get; init; }
    public required DateTime DateTime { get; init; }
    public Guid? CorrelationId { get; init; }
    public string? Topic { get; init; }

    internal static MessageMeta FromMessage(IMessage message) => new()
    {
        MessageId = message.MessageId,
        DateTime = message.Datetime,
        Queue = message.QueueUrl,
        Topic = message.TopicArn,
        CorrelationId = message.CorrelationId
    };
}

public interface IWeakConsumer
{
    internal Task Consume(object message, MessageMeta meta, CancellationToken ctx);
}

public interface IMessageConsumer<in TMessage> : IWeakConsumer where TMessage : notnull
{
    Task IWeakConsumer.Consume(object message, MessageMeta meta, CancellationToken ctx) =>
        Consume((TMessage)message, meta, ctx);

    Task Consume(TMessage message, MessageMeta meta, CancellationToken ctx);
}

public interface IConsumer<in TMessage> : IMessageConsumer<TMessage> where TMessage : notnull
{
    Task IMessageConsumer<TMessage>.
        Consume(TMessage message, MessageMeta meta, CancellationToken ctx) =>
        Consume(message, ctx);

    Task Consume(TMessage message, CancellationToken ctx);
}

public interface IMessageConsumer : IMessageConsumer<string>
{
}
