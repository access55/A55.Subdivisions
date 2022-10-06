using System.Text.RegularExpressions;
using A55.Subdivisions.Aws.Adapters;
using Amazon.EventBridge;
using Amazon.EventBridge.Model;
using Bogus;

namespace A55.Subdivisions.Aws.Tests.Builders;

public class EventRuleBuilder
{
    Faker faker = new();

    internal EventName Event { get; private set; }
    public string TopicName { get; }
    public string EventName { get; }

    string state = RuleState.ENABLED;
    public EventRuleBuilder()
    {
        var firstPart = faker.Person.FirstName;
        var secondPart = faker.Person.LastName;
        
        EventName  = $"{firstPart.ToLowerInvariant()}_{secondPart.ToLowerInvariant()}";
        TopicName = $"{firstPart}{secondPart}";
        Event = new(EventName);
    }

    public EventRuleBuilder Disabled()
    {
        state = RuleState.DISABLED;
        return this;
    }

    public string EventPattern => $@"
{{
  ""detail-type"": [""{EventName}""],
  ""detail"": {{
    ""event"": [""{EventName}""]
  }}
}}";

    public PutRuleRequest CreateRule() => new()
    {
        Name = TopicName,
        Description = faker.Lorem.Paragraph(),
        State = state,
        EventBusName = "default",
        EventPattern = Regex.Replace(EventPattern, @"\r\n?|\n", string.Empty),
    };
}