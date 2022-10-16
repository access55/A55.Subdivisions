using System.Text.Json;
using System.Text.Json.Nodes;
using FluentAssertions;
using FluentAssertions.Equivalency;
using FluentAssertions.Execution;
using FluentAssertions.Primitives;

namespace Subdivisions.Testing;

public class FakeBrokerAssertions : ReferenceTypeAssertions<IFakeBroker, FakeBrokerAssertions>
{
    public FakeBrokerAssertions(IFakeBroker subject) : base(subject)
    {
    }

    protected override string Identifier => "fakebroker";

    public AndConstraint<FakeBrokerAssertions> HaveAnyMessage(
        string topicName, string because = "", params object[] becauseArgs)
    {
        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(!string.IsNullOrEmpty(topicName))
            .FailWith("You can't assert a message was produced if you don't pass a proper topic name")
            .Then
            .Given(() => Subject.ProducedOn(topicName))
            .ForCondition(messages => messages.Length > 0)
            .FailWith("Expected {0} to contain messages{reason}, but not found any.",
                _ => topicName);

        return new AndConstraint<FakeBrokerAssertions>(this);
    }

    public AndConstraint<FakeBrokerAssertions> HaveJsonMessage(
        string topicName, string message, string because = "", params object[] becauseArgs)
    {
        var jsonMessage = JsonNode.Parse(message);
        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(!string.IsNullOrEmpty(topicName))
            .FailWith("You can't assert a message was produced if you don't pass a proper topic name")
            .Then
            .Given(() => Subject.ProducedOn(topicName))
            .ForCondition(messages => messages
                .Select(x => JsonNode.Parse(x)?.ToJsonString())
                .Any(x => x == jsonMessage?.ToJsonString()))
            .FailWith("Expected {0} to contain {1}{reason}, but found {2}",
                _ => topicName, _ => jsonMessage?.ToJsonString(), m => string.Join(",", m));

        return new AndConstraint<FakeBrokerAssertions>(this);
    }

    public AndConstraint<FakeBrokerAssertions> HaveMessage(
        string topicName, object message, string because = "", params object[] becauseArgs)
    {
        var jsonMessage = message as string ?? JsonSerializer.Serialize(message);
        return HaveJsonMessage(topicName, jsonMessage, because, becauseArgs);
    }

    public AndConstraint<FakeBrokerAssertions> HaveMessages(
        Dictionary<string, object[]> messages, string because = "", params object[] becauseArgs)
    {
        var parsedMessages = messages.ToDictionary(x => x.Key,
            x => x.Value.Select(v => v as string ?? JsonSerializer.Serialize(v)).ToArray());

        var pushedMessages = Subject.ProducedMessages();
        pushedMessages.Should().BeEquivalentTo(parsedMessages, because, becauseArgs);

        return new AndConstraint<FakeBrokerAssertions>(this);
    }
}

public static class FakeBrokerAssertionsExtensions
{
    public static FakeBrokerAssertions Should(this IFakeBroker instance) => new(instance);
}
