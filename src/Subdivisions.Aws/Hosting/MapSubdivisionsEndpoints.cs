using System.Reflection;
using System.Text.Json.Nodes;
using CorrelationId.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Subdivisions.Models;
using Subdivisions.Services;

namespace Subdivisions.Hosting;

static class SubdivisionsEndpoints
{
    public static void MapTopicEndpoints(this IEndpointRouteBuilder app, SubConfig config)
    {
        if (!config.MapConsumerEndpoints)
            return;

        var topics = app.ServiceProvider
            .GetRequiredService<IEnumerable<IConsumerDescriber>>()
            .ToArray();

        var serializer = app.ServiceProvider.GetRequiredService<ISubMessageSerializer>();
        foreach (var topic in topics)
        {
            var info = new TopicId(topic.TopicName, config);
            var builder = app.MapPost($"/consumer/{topic.TopicName}", async (
                [FromServices] IProducerClient producer, HttpContext context
            ) =>
            {
                var requestMessage = await context.Request.ReadFromJsonAsync(topic.MessageType);
                var message = serializer.Serialize(requestMessage);
                var correlationId = Guid.TryParse(app.ServiceProvider
                    .GetService<ICorrelationContextAccessor>()
                    ?.CorrelationContext
                    ?.CorrelationId, out var corrId)
                    ? corrId
                    : default(Guid?);

                var result = await producer.Publish(topic.TopicName, message, correlationId);
                TypedResults.Ok(result);
            });
            builder
                .Accepts(topic.MessageType, "application/json")
                .WithTags("Subdivisions")
                .WithDescription(GetDescription(info, topic, serializer))
                .WithOpenApi(options =>
                {
                    options.Parameters.Clear();
                    options.RequestBody.Required = true;
                    return options;
                });
        }
    }

    static string GetDescription(TopicId info, IConsumerDescriber topic, ISubMessageSerializer serializer) =>
        $"""
        <b>topic:</b> {info.TopicName}<br/>
        <b>queue:</b> {info.QueueName}<br/>
        <b>event:</b> {info.Event}<br/>
        <hr/>
        <b>PubSub Contract:</b><br/>
        <pre>
        {GenerateJsonSample(topic.MessageType, serializer)}
        </pre>
        """;

    static string GenerateJsonSample(Type type, ISubMessageSerializer serializer)
    {
        try
        {
            var sample = GenerateSample(type);
            var node = JsonNode.Parse(serializer.Serialize(sample));
            return node!.ToString();
        }
        catch
        {
            return string.Empty;
        }
    }

    static object? GenerateSample(Type type)
    {
        object? GetDefault(Type t)
        {
            try
            {
                if (t == typeof(string))
                    return "string";
                if (t == typeof(Guid))
                    return Guid.Empty;
                if (t.IsPrimitive)
                    return Activator.CreateInstance(t);

                if (GenerateSample(t) is { } value)
                    return value;

                if (t.IsValueType)
                    return Activator.CreateInstance(t);
            }
            catch
            {
                return null;
            }

            return null;
        }

        var constructors = type.GetConstructors()
            .OrderByDescending(ctor => ctor.GetParameters().Length)
            .ToList();

        foreach (var ctor in constructors)
        {
            try
            {
                var parameters =
                    ctor.GetParameters()
                        .Select(param => param.ParameterType)
                        .Select(GetDefault)
                        .ToArray();
                var generatedObject = ctor.Invoke(parameters);
                var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                foreach (var prop in props)
                {
                    var value = prop.GetValue(generatedObject);
                    if ((!prop.PropertyType.IsValueType && value is not null)
                        || (prop.PropertyType.IsValueType &&
                            !value!.Equals(Activator.CreateInstance(prop.PropertyType))))
                        continue;

                    var propValue = GetDefault(prop.PropertyType);
                    prop.SetValue(generatedObject, propValue);
                }

                return generatedObject;
            }
            catch
            {
                return null;
            }
        }

        return null;
    }
}
