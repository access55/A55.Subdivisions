global using IMessage = Subdivisions.Models.IMessage<string>;

namespace Subdivisions.Models;

public interface IMessage<out TBody> where TBody : notnull
{
    Guid? MessageId { get; }
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
    public TBody Body { get; }
    public uint RetryNumber { get; }

    internal Message(
        Guid? id,
        in TBody body,
        DateTime datetime,
        Func<Task> deleteMessage,
        Func<TimeSpan, Task> releaseMessage,
        uint retryNumber = 0)
    {
        MessageId = id;
        Body = body;
        Datetime = datetime;
        this.deleteMessage = deleteMessage;
        this.releaseMessage = releaseMessage;
        RetryNumber = retryNumber;
    }

    public Task Delete() => deleteMessage();
    public Task Release(TimeSpan delay) => releaseMessage(delay);

    public IMessage<TMap> Map<TMap>(Func<TBody, TMap> selector) where TMap : notnull =>
        new Message<TMap>(MessageId, selector(Body), Datetime, Delete, Release, RetryNumber);
}
