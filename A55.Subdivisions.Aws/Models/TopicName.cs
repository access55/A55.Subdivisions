using System.Reflection;
using A55.Subdivisions.Aws.Extensions;

namespace A55.Subdivisions.Aws;

class TopicName
{
    public TopicName(string topic, string prefix = "", string sufix = "", string? source = null)
    {
        if (!IsValidTopicName(topic))
            throw new ArgumentException($"Invalid topic name {topic}", nameof(topic));

        Prefix = prefix;
        Sufix = sufix;

        Topic = topic.PascalToSnakeCase();
        Source = source?.PascalToSnakeCase() ?? DefaultSourceName();

        FullTopicName =
            $"{Prefix}{Topic.SnakeToPascalCase()}{Sufix}";

        FullQueueName =
            $"{Prefix.SnakeToPascalCase()}_{Source}_{Topic}{Sufix.SnakeToPascalCase()}";
    }

    static string DefaultSourceName() =>
        Assembly.GetExecutingAssembly().GetName().Name?.ToLowerInvariant().Replace(".", "_")
        ?? throw new InvalidOperationException("Unable to infer the source name");

    static bool IsValidTopicName(string topic) =>
        topic.Length >= 6
        && char.IsLetter(topic[0])
        && topic.All(c => char.IsLetterOrDigit(c) || c is '_');

    public string FullTopicName { get; }
    public string FullQueueName { get; }

    public string Topic { get; }
    public string Source { get; }
    public string Prefix { get; }
    public string Sufix { get; }

    public override string ToString() => FullTopicName;
}
