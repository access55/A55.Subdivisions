namespace A55.Subdivisions.Aws;

record TopicName(string Topic, string Prefix = "", string Sufix = "")
{
    public string FullName { get; } =
        $"{Prefix.SnakeToPascalCase()}{Topic.SnakeToPascalCase()}{Sufix.SnakeToPascalCase()}".Trim();

    public override string ToString() => FullName;
    public static implicit operator string(TopicName topic) => topic.FullName;
}

record BaseArn(string Value)
{
    public override string ToString() => Value;
}
