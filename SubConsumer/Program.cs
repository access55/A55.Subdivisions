using A55.Subdivisions;
using SubConsumer;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSubdivisions(sub =>
{
    sub.MapTopic<MyMessage>("my_topic")
        .WithConsumer((MyMessage message, ILogger<Program> logger) =>
        {
            logger.LogInformation("Received {Message}", message.ToString());
        });
    // .WithConsumer<MyConsumer>();
});

var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.Run();
