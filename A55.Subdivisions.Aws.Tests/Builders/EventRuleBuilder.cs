using System.Text.RegularExpressions;
using Amazon.EventBridge;
using Amazon.EventBridge.Model;
using Bogus;

namespace A55.Subdivisions.Aws.Tests.Builders;

public class EventRuleBuilder
{
    readonly Faker faker = new();

    string state = RuleState.ENABLED;

    public EventRuleBuilder()
    {
        var firstPart = faker.Person.FirstName;
        var secondPart = $"{faker.Person.LastName}{faker.Random.Replace("####")}";

        EventName = $"{firstPart.ToLowerInvariant()}_{secondPart.ToLowerInvariant()}";
        TopicName = $"{firstPart}{secondPart}";
        Topic = new(EventName);
    }

    internal TopicName Topic { get; }
    public string TopicName { get; }
    public string EventName { get; }

    public string EventPattern => $@"
{{
  ""detail-type"": [""{EventName}""],
  ""detail"": {{
    ""event"": [""{EventName}""]
  }}
}}";

    public EventRuleBuilder Disabled()
    {
        state = RuleState.DISABLED;
        return this;
    }

    public PutRuleRequest CreateRule() => new()
    {
        Name = TopicName,
        Description = faker.Lorem.Paragraph(),
        State = state,
        EventBusName = "default",
        EventPattern = Regex.Replace(EventPattern, @"\r\n?|\n", string.Empty)
    };
}
