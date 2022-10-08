namespace A55.Subdivisions.Aws.Models;

public record MessagePayload(string Event, DateTime DateTime, string Payload);

public sealed class Message
{
    public Guid Id { get; }

    readonly MessagePayload payload;
    readonly Func<Task> deleteMessage;

    public Message(
        Guid Id,
        MessagePayload payload,
        Func<Task> deleteMessage)
    {
        this.Id = Id;
        this.payload = payload;
        this.deleteMessage = deleteMessage;
    }

    public string Body => payload.Payload;
    public DateTime Datetime => payload.DateTime;

    public Task Delete() => deleteMessage();
}
