using System.Runtime.Serialization;
using System.Text.Json;

namespace A55.Subdivisions.Aws;

public interface ISubMessageSerializer
{
    string Serialize<TValue>(TValue something);
    TValue Deserialize<TValue>(ReadOnlySpan<char> json);
}

public class SubJsonSerializer : ISubMessageSerializer
{
    static readonly JsonSerializerOptions jsonOptions = new() {PropertyNamingPolicy = JsonNamingPolicy.CamelCase};

    public string Serialize<TValue>(TValue something) =>
        JsonSerializer.Serialize(something, jsonOptions);

    public TValue Deserialize<TValue>(ReadOnlySpan<char> json) =>
        JsonSerializer.Deserialize<TValue>(json, jsonOptions) ??
        throw new SerializationException("Unable to deserialize message");
}
