namespace A55.Subdivisions.Aws.Models;

public record MessagePayload(string Event, DateTime DateTime, string Payload);

public sealed class Message : Message<string>
{
    internal Message(Guid id, string body, DateTime datetime, Func<Task> deleteMessage)
        : base(id, body, datetime, deleteMessage)
    {
    }
}

public class Message<T> where T : notnull
{
    public DateTime Datetime { get; }
    public Guid Id { get; }
    public T Body { get; }

    readonly Func<Task> deleteMessage;

    internal Message(
        Guid id,
        in T body,
        DateTime datetime,
        Func<Task> deleteMessage)
    {
        Id = id;
        Body = body;
        Datetime = datetime;
        this.deleteMessage = deleteMessage;
    }

    public Task Delete() => deleteMessage();

    internal Message<TMap> Map<TMap>(Func<T, TMap> selector) where TMap : notnull =>
        new(id: Id, body: selector(Body), datetime: Datetime, deleteMessage);
}
