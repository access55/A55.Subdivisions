namespace A55.Subdivisions.Aws.Models;

public record MessagePayload(string Event, DateTime DateTime, string Payload);

public sealed class Message
{
    readonly Func<Task> deleteMessage;

    readonly MessagePayload payload;

    public Message(
        Guid Id,
        MessagePayload payload,
        Func<Task> deleteMessage)
    {
        this.Id = Id;
        this.payload = payload;
        this.deleteMessage = deleteMessage;
    }

    public Guid Id { get; }

    public string Body => payload.Payload;
    public DateTime Datetime => payload.DateTime;

    public Task Delete() => deleteMessage();
}
