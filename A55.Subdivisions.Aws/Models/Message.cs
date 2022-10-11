global using IMessage = A55.Subdivisions.Aws.Models.IMessage<string>;

namespace A55.Subdivisions.Aws.Models;

public interface IMessage<out TBody> where TBody : notnull
{
    DateTime Datetime { get; }
    Guid Id { get; }
    TBody Body { get; }
    Task Delete();
    Task Release();

    IMessage<TMap> Map<TMap>(Func<TBody, TMap> selector) where TMap : notnull;
}

readonly struct Message<TBody> : IMessage<TBody> where TBody : notnull
{
    readonly Func<Task> deleteMessage;
    readonly Func<Task> releaseMessage;

    internal Message(
        Guid id,
        in TBody body,
        DateTime datetime,
        Func<Task> deleteMessage,
        Func<Task> releaseMessage
    )
    {
        Id = id;
        Body = body;
        Datetime = datetime;
        this.deleteMessage = deleteMessage;
        this.releaseMessage = releaseMessage;
    }

    public DateTime Datetime { get; }
    public Guid Id { get; }
    public TBody Body { get; }

    public Task Delete() => deleteMessage();
    public Task Release() => releaseMessage();

    public IMessage<TMap> Map<TMap>(Func<TBody, TMap> selector) where TMap : notnull =>
        new Message<TMap>(Id, selector(Body), Datetime, Delete, Release);
}
