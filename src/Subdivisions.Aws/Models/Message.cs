global using IMessage = Subdivisions.Models.IMessage<string>;

namespace Subdivisions.Models;

public interface IMessage<out TBody> where TBody : notnull
{
    Guid? MessageId { get; }
    Guid? CorrelationId { get; }
    DateTime Datetime { get; }
    TBody Body { get; }
    uint RetryNumber { get; }
    string QueueUrl { get; }
    Task Delete();
    Task Release(TimeSpan delay);

    IMessage<TMap> Map<TMap>(Func<TBody, TMap> selector) where TMap : notnull;
}

readonly struct Message<TBody> : IMessage<TBody> where TBody : notnull
{
    readonly Func<Task> deleteMessage;
    readonly Func<TimeSpan, Task> releaseMessage;

    public DateTime Datetime { get; }
    public Guid? MessageId { get; }
    public Guid? CorrelationId { get; }
    public TBody Body { get; }
    public uint RetryNumber { get; }

    public string QueueUrl { get; }

    internal Message(
        Guid? id,
        in TBody body,
        DateTime datetime,
        Func<Task> deleteMessage,
        Func<TimeSpan, Task> releaseMessage,
        Guid? correlationId,
        string queueUrl,
        uint retryNumber = 0
    )
    {
        MessageId = id;
        Body = body;
        Datetime = datetime;
        CorrelationId = correlationId;
        RetryNumber = retryNumber;
        QueueUrl = queueUrl;
        this.deleteMessage = deleteMessage;
        this.releaseMessage = releaseMessage;
    }

    public Task Delete() => deleteMessage();
    public Task Release(TimeSpan delay) => releaseMessage(delay);

    public IMessage<TMap> Map<TMap>(Func<TBody, TMap> selector) where TMap : notnull =>
        new Message<TMap>(MessageId, selector(Body), Datetime, Delete, Release, CorrelationId, QueueUrl, RetryNumber);
}

record MessageEnvelope(
    string Event,
    DateTime DateTime,
    string Payload,
    bool? Compressed = null,
    Guid? MessageId = null,
    Guid? CorrelationId = null);
