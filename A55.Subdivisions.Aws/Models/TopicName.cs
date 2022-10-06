using A55.Subdivisions.Aws.Extensions;

namespace A55.Subdivisions.Aws;

class TopicName
{
    public TopicName(string topic, string prefix = "", string sufix = "")
    {
        if (!IsValidTopicName(topic))
            throw new ArgumentException($"Invalid topic name {topic}", nameof(topic));

        Topic = topic.PascalToSnakeCase();
        Prefix = prefix;
        Sufix = sufix;
        FullNamePascalCase =
            $"{Prefix}{Topic.SnakeToPascalCase()}{Sufix}";
    }

    static bool IsValidTopicName(string topic) =>
        topic.Length >= 6
        && char.IsLetter(topic[0])
        && topic.All(c => char.IsLetterOrDigit(c) || c is '_');

    public string FullNamePascalCase { get; }

    public string Topic { get; }
    public string Prefix { get; }
    public string Sufix { get; }

    public override string ToString() => FullNamePascalCase;
    public static implicit operator string(TopicName topic) => topic.FullNamePascalCase;
}
