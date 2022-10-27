using CorrelationId;
using Subdivisions;
using SubPublisher;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSubdivisions(sub =>
{
    sub.MapTopic<MyMessage>("my_topic");
});

var app = builder.Build();
app.UseCorrelationId();

app.MapGet("/publish/{name}", async (
    IProducer<MyMessage> publisher,
    ILogger<Program> logger,
    string name
) =>
{
    logger.LogInformation("Publishing message");
    var message = new MyMessage
    {
        Id = Guid.NewGuid(),
        Name = name,
        BirthDate = DateTime.Now,
        Age = Random.Shared.Next()
    };
    await publisher.Publish(message);
});

app.MapGet("/", () => "Hello Publisher!");

app.Run();
