using Subdivisions.Services;

namespace Subdivisions.Aws.Tests.TestUtils;

class FakeSerializer : ISubMessageSerializer
{
    readonly object? returnValue;

    public FakeSerializer(object? returnValue) => this.returnValue = returnValue;
    public string Serialize<TValue>(TValue something) => default!;

    public TValue Deserialize<TValue>(ReadOnlySpan<char> json) => (TValue)returnValue!;

    public object? Deserialize(Type type, ReadOnlySpan<char> json) => returnValue;
}
