﻿using A55.Subdivisions.Aws.Extensions;

namespace A55.Subdivisions.Aws.Models;

class TopicName
{
    public TopicName(string topic, SubTopicNameConfig config)
    {
        if (!IsValidTopicName(topic))
            throw new ArgumentException($"Invalid topic name {topic}", nameof(topic));

        Prefix = config.Prefix.PascalToSnakeCase();
        Suffix = config.Suffix.PascalToSnakeCase();
        Topic = topic.PascalToSnakeCase();
        Source = (config.Source ?? config.FallbackSource)?.PascalToSnakeCase() ??
                 throw new InvalidOperationException("Unable to infer the source name");

        FullTopicName =
            $"{Prefix.SnakeToPascalCase()}{Topic.SnakeToPascalCase()}{Suffix.SnakeToPascalCase()}";

        FullQueueName =
            $"{Prefix}_{Source}_{Topic}_{Suffix}".TrimUnderscores();
    }

    public string FullTopicName { get; }
    public string FullQueueName { get; }

    public string Topic { get; }
    public string Source { get; }
    public string Prefix { get; }
    public string Suffix { get; }

    static bool IsValidTopicName(string topic) =>
        topic.Length >= 6
        && char.IsLetter(topic[0])
        && topic.All(c => char.IsLetterOrDigit(c) || c is '_');

    public override string ToString() => FullTopicName;
}
