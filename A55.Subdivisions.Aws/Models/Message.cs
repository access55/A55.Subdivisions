namespace A55.Subdivisions.Aws.Models;

public record MessagePayload(string Event, DateTime DateTime, string Payload);

public sealed class Message : Message<string>
{
    internal Message(Guid id, string body, DateTime datetime, Func<Task> deleteMessage, Func<Task> releaseMessage)
        : base(id, body, datetime, deleteMessage, releaseMessage)
    {
    }
}

public class Message<T> where T : notnull
{
    readonly Func<Task> deleteMessage;
    readonly Func<Task> releaseMessage;

    internal Message(
        Guid id,
        in T body,
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
    public T Body { get; }

    public Task Delete() => deleteMessage();
    public Task Release() => releaseMessage();

    internal Message<TMap> Map<TMap>(Func<T, TMap> selector) where TMap : notnull =>
        new(Id, selector(Body), Datetime, deleteMessage,
            releaseMessage);
}
