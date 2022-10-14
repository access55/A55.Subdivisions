using Subdivisions.Extensions;

namespace Subdivisions.Models;

class TopicName
{
    public TopicName(string topic, SubTopicNameConfig config)
    {
        if (!IsValidTopicName(topic))
            throw new ArgumentException($"Invalid topic name {topic}", nameof(topic));

        Prefix = config.Prefix.ToSnakeCase();
        Suffix = config.Suffix.ToSnakeCase();
        Topic = topic.ToSnakeCase();
        Source = (config.Source ?? config.FallbackSource)?.ToSnakeCase() ??
                 throw new InvalidOperationException("Unable to infer the source name");

        FullTopicName =
            $"{Prefix.ToPascalCase()}{Topic.ToPascalCase()}{Suffix.ToPascalCase()}";

        FullQueueName =
            $"{Prefix}_{Source}_{Topic}_{Suffix}".TrimUnderscores();
    }

    public string FullTopicName { get; }
    public string FullQueueName { get; }

    public string Topic { get; }
    public string Source { get; }
    public string Prefix { get; }
    public string Suffix { get; }

    public static bool IsValidTopicName(string topic) =>
        topic.Length >= 6
        && char.IsLetter(topic[0])
        && topic.All(c => char.IsLetterOrDigit(c) || c is '_');

    public override string ToString() => FullTopicName;
}
