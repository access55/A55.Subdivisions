using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using SubConsumer;
using Subdivisions;
using Subdivisions.Hosting;

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
    sub.MapTopic<MyMessage>("my_topic")
        .WithConsumer<MyConsumer>();

    sub.MapTopic<MyMessage2>("my_other_topic")
        .WithConsumer((MyMessage2 message, ILogger<Program> logger) =>
            logger.LogInformation("Received: {Message}", message.ToString()));
});

var app = builder.Build();

app.MapGet("/", () => "Hello Consumer!");
app.MapHealthChecks("health");
app.Run();
