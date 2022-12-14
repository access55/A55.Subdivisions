using CorrelationId;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Subdivisions;
using Subdivisions.Hosting;
using SubPublisher;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddOpenTelemetryTracing(b => b
        .AddAspNetCoreInstrumentation()
        .AddSource("A55.Subdivisions")
        .AddConsoleExporter())
    .AddOpenTelemetryMetrics(b => b
        .AddAspNetCoreInstrumentation()
        .AddMeter("A55.Subdivisions")
        .AddConsoleExporter());

builder.Services
    .AddHealthChecks()
    .AddCheck<SubdivisionsHealthCheck>("Subdivisions");

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
    await publisher.TryPublish(message);
});

app.MapGet("/", () => "Hello Publisher!");
app.MapHealthChecks("health");

app.Run();
