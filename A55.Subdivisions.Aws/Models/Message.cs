global using IMessage = A55.Subdivisions.Models.IMessage<string>;

namespace A55.Subdivisions.Models;

public interface IMessage<out TBody> where TBody : notnull
{
    Guid? MessageId { get; }
    DateTime Datetime { get; }
    TBody Body { get; }
    Task Delete();
    Task Release();
    uint RetryNumber { get; }

    IMessage<TMap> Map<TMap>(Func<TBody, TMap> selector) where TMap : notnull;
}

readonly struct Message<TBody> : IMessage<TBody> where TBody : notnull
{
    readonly Func<Task> deleteMessage;
    readonly Func<Task> releaseMessage;

    public DateTime Datetime { get; }
    public Guid? MessageId { get; }
    public TBody Body { get; }
    public uint RetryNumber { get; }

    internal Message(
        Guid? id,
        in TBody body,
        DateTime datetime,
        Func<Task> deleteMessage,
        Func<Task> releaseMessage,
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
    public Task Release() => releaseMessage();

    public IMessage<TMap> Map<TMap>(Func<TBody, TMap> selector) where TMap : notnull =>
        new Message<TMap>(MessageId, selector(Body), Datetime, Delete, Release, RetryNumber);
}
