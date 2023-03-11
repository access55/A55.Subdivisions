using CorrelationId;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Subdivisions;
using Subdivisions.Hosting;
using SubPublisher;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddOpenTelemetry()
    .WithTracing(b => b
        .AddAspNetCoreInstrumentation()
        .AddSource(SubTelemetry.SourceName)
        .AddConsoleExporter()
    )
    .WithMetrics(b => b
        .AddAspNetCoreInstrumentation()
        .AddMeter(SubTelemetry.SourceName)
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
