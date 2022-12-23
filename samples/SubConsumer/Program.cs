using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using SubConsumer;
using Subdivisions;
using Subdivisions.Hosting;

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
