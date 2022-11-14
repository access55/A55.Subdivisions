using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Subdivisions.Extensions;

namespace Subdivisions.Services;

public interface ISubMessageSerializer
{
    string Serialize<TValue>(TValue something);
    TValue Deserialize<TValue>(ReadOnlySpan<char> json);
    object? Deserialize(Type type, ReadOnlySpan<char> json);
}

class SnakeCaseNamingPolicy : JsonNamingPolicy
{
    public static SnakeCaseNamingPolicy Instance { get; } = new();
    public override string ConvertName(string name) => name.ToSnakeCase();
}

class SubJsonSerializer : ISubMessageSerializer
{
    static readonly JsonSerializerOptions jsonOptions = new()
    {
        PropertyNamingPolicy = SnakeCaseNamingPolicy.Instance,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(), new DateTimeUtcOnlyConverter() }
    };

    public string Serialize<TValue>(TValue something) =>
        JsonSerializer.Serialize(something, jsonOptions);

    public TValue Deserialize<TValue>(ReadOnlySpan<char> json) =>
        JsonSerializer.Deserialize<TValue>(json, jsonOptions) ??
        throw new SerializationException("Unable to deserialize message");

    public object Deserialize(Type type, ReadOnlySpan<char> json) =>
        JsonSerializer.Deserialize(json, type, jsonOptions) ??
        throw new SerializationException("Unable to deserialize message");
}
