using SubConsumer;
using Subdivisions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSubdivisions(sub =>
{
    sub.MapTopic<MyMessage>("my_topic")
         .WithConsumer<MyConsumer>();

    sub.MapTopic<MyMessage2>("my_other_topic")
         .WithConsumer((MyMessage2 message, ILogger<Program> logger) =>
            logger.LogInformation("Received: {Message}", message.ToString()));
});

var app = builder.Build();

app.MapGet("/", () => "Hello Consumer!");

app.Run();
