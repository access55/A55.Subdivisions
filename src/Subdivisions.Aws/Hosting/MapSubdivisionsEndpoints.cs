using System.Collections.ObjectModel;
using System.Globalization;
using System.Net.Mime;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using CorrelationId.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Writers;
using Subdivisions.Models;
using Subdivisions.Services;
using Swashbuckle.AspNetCore.SwaggerGen;
using JsonOptions = Microsoft.AspNetCore.Http.Json.JsonOptions;

namespace Subdivisions.Hosting;

static class SubdivisionsEndpoints
{
    public static void MapTopicEndpoints(this IEndpointRouteBuilder app, SubConfig config)
    {
        if (!config.MapConsumerEndpoints)
            return;

        var topics = app.ServiceProvider.GetRequiredService<IEnumerable<IConsumerDescriber>>();
        var serializer = app.ServiceProvider.GetRequiredService<ISubMessageSerializer>();
        foreach (var topic in topics)
            app.MapPost($"/consumer/{topic.TopicName}", async (
                    [FromServices] ISubMessageSerializer serializer,
                    [FromServices] IProducerClient producer,
                    [FromBody] JsonDocument body
                ) =>
                {
                    var sample = GenerateSample(topic.MessageType);
                    return TypedResults.Ok(sample);
                    // var services = context.RequestServices;
                    // var requestMessage = await context.Request.ReadFromJsonAsync(topic.MessageType);
                    // var message = serializer.Serialize(requestMessage);
                    //
                    // var correlationId = Guid.TryParse(services
                    //     .GetService<ICorrelationContextAccessor>()
                    //     ?.CorrelationContext
                    //     ?.CorrelationId, out var corrId)
                    //     ? corrId
                    //     : default(Guid?);
                    //
                    // var result = await producer.Publish(topic.TopicName, message, correlationId);
                    // TypedResults.Ok(result);
                })
                // .Accepts(topic.MessageType, "application/json")
                .WithOpenApi(generatedOperation =>
                {
                    generatedOperation.Parameters.Clear();
                    generatedOperation.SerializeAsV3(new OpenApiJsonWriter(new StringWriter(),
                        new OpenApiJsonWriterSettings()));
                    generatedOperation.RequestBody = new OpenApiRequestBody
                    {
                        Required = true,
                        Content = new Dictionary<string, OpenApiMediaType>()
                        {
                            [MediaTypeNames.Application.Json] =
                                new() {Example = GetOpenApiSample(serializer, topic.MessageType), Schema = null,}
                        }
                    };
                    return generatedOperation;
                });
    }

    static IOpenApiAny GetOpenApiSample(ISubMessageSerializer serializer, Type messageType)
    {
        var value = GenerateSample(messageType);
        if (value is null || serializer.Serialize(value) is not { } json || JsonDocument.Parse(json) is not { } node)
            return new OpenApiNull();

        var openApiObject = ParseJsonNode(node.RootElement);
        return openApiObject;
    }

    static IOpenApiAny ParseJsonNode(JsonElement node)
    {
        OpenApiObject GetObject(JsonElement obj)
        {
            var result = new OpenApiObject();
            foreach (var prop in obj.EnumerateObject())
                result.Add(prop.Name, ParseJsonNode(prop.Value));
            return result;
        }

        OpenApiArray GetArray(JsonElement obj)
        {
            var result = new OpenApiArray();
            result.AddRange(obj.EnumerateArray().Select(ParseJsonNode));
            return result;
        }

        return node.ValueKind switch
        {
            JsonValueKind.Undefined => new OpenApiNull(),
            JsonValueKind.String => node.GetString() is var str && DateTime.TryParse(str, out var date)
                ? new OpenApiDateTime(date)
                : new OpenApiString(str),
            JsonValueKind.Number => node.GetDouble() switch
            {
                NumberHooks.Double => new OpenApiDouble(0.0),
                NumberHooks.Float => new OpenApiFloat(0.0f),
                NumberHooks.Int => new OpenApiFloat(0),
                NumberHooks.Long => new OpenApiFloat(0),
                NumberHooks.Byte => new OpenApiByte(0),
                _ => new OpenApiInteger(0),
            },
            JsonValueKind.True => new OpenApiBoolean(true),
            JsonValueKind.False => new OpenApiBoolean(false),
            JsonValueKind.Null => new OpenApiNull(),
            JsonValueKind.Object => GetObject(node),
            JsonValueKind.Array => GetArray(node),
            _ => new OpenApiNull()
        };
    }

    static class NumberHooks
    {
        public const float Double = 0.1f;
        public const float Float = 0.2f;
        public const float Int = 0f;
        public const float Long = 1f;
        public const float Byte = 2f;
    }

    static object? GenerateSample(Type type)
    {
        object? GetDefault(Type t)
        {
            try
            {
                if (t == typeof(string))
                    return "string";
                if (t == typeof(DateTime))
                    return DateTime.MinValue;
                if (t == typeof(Guid))
                    return Guid.NewGuid();
                if (t == typeof(decimal) || t == typeof(double))
                    return NumberHooks.Double;
                if (t == typeof(float))
                    return NumberHooks.Float;
                if (t == typeof(int) || t == typeof(short))
                    return NumberHooks.Int;
                if (t == typeof(byte))
                    return NumberHooks.Byte;
                if (t == typeof(long))
                    return NumberHooks.Long;
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
