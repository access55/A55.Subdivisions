global using IMessage = Subdivisions.Models.IMessage<string>;

namespace Subdivisions.Models;

public interface IMessage<out TBody> where TBody : notnull
{
    Guid? MessageId { get; }
    Guid? CorrelationId { get; }
    DateTime Datetime { get; }
    TBody Body { get; }
    uint RetryNumber { get; }
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

    internal Message(
        Guid? id,
        in TBody body,
        DateTime datetime,
        Func<Task> deleteMessage,
        Func<TimeSpan, Task> releaseMessage,
        Guid? correlationId,
        uint retryNumber = 0)
    {
        MessageId = id;
        Body = body;
        Datetime = datetime;
        CorrelationId = correlationId;
        RetryNumber = retryNumber;
        this.deleteMessage = deleteMessage;
        this.releaseMessage = releaseMessage;
    }

    public Task Delete() => deleteMessage();
    public Task Release(TimeSpan delay) => releaseMessage(delay);

    public IMessage<TMap> Map<TMap>(Func<TBody, TMap> selector) where TMap : notnull =>
        new Message<TMap>(MessageId, selector(Body), Datetime, Delete, Release, CorrelationId, RetryNumber);
}

record MessageEnvelope(
    string Event,
    DateTime DateTime,
    string Payload,
    bool? Compressed,
    Guid? MessageId = null,
    Guid? CorrelationId = null);
