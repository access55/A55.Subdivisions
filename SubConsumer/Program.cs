using A55.Subdivisions;
using SubConsumer;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSubdivisions(sub =>
{
    sub.MapTopic<MyMessage>("my_topic")
        .WithConsumer<MyConsumer>();
});

var app = builder.Build();

app.MapGet("/", () => "Hello Consumer!");

app.Run();
