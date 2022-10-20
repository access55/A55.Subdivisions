using System.Text.Json;
using FluentAssertions;
using FluentAssertions.Execution;
using FluentAssertions.Json;
using FluentAssertions.Primitives;
using Newtonsoft.Json.Linq;

namespace Subdivisions.Testing;

public class FakeBrokerAssertions : ReferenceTypeAssertions<IFakeReadonlyBroker, FakeBrokerAssertions>
{
    public FakeBrokerAssertions(IFakeReadonlyBroker subject) : base(subject)
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

    public AndConstraint<JTokenAssertions> Topic(
        string topicName)
    {
        var messages = JArray.Parse($"[{string.Join(",", Subject.ProducedOn(topicName))}]");
        return new AndConstraint<JTokenAssertions>(new(messages));
    }

    #region ContainsMessage

    AndConstraint<FakeBrokerAssertions> ContainMessageAssert(
        string topicName, JToken message, bool not,
        string because = "", params object[] becauseArgs)
    {
        var itemOnArray = new JArray(message);
        if (not)
            Topic(topicName).And.NotBeEquivalentTo(itemOnArray, because, becauseArgs);
        else
            Topic(topicName).And.BeEquivalentTo(itemOnArray, because, becauseArgs);

        return new AndConstraint<FakeBrokerAssertions>(this);
    }

    public AndConstraint<FakeBrokerAssertions> ContainMessage(
        string topicName, object message,
        string because = "", params object[] becauseArgs)
    {
        var jsonMessage = JsonSerializer.Serialize(message);
        return ContainMessage(topicName, jsonMessage, because, becauseArgs);
    }

    public AndConstraint<FakeBrokerAssertions> ContainMessage(
        string topicName, string message,
        string because = "", params object[] becauseArgs)
    {
        var jsonMessage = JToken.Parse(message);
        return ContainMessage(topicName, jsonMessage, because, becauseArgs);
    }

    public AndConstraint<FakeBrokerAssertions> ContainMessage(
        string topicName, JToken message,
        string because = "", params object[] becauseArgs) =>
        ContainMessageAssert(topicName, message, not: false, because, becauseArgs);

    public AndConstraint<FakeBrokerAssertions> NotContainMessage(
        string topicName, object message,
        string because = "", params object[] becauseArgs)
    {
        var jsonMessage = JsonSerializer.Serialize(message);
        return NotContainMessage(topicName, jsonMessage, because, becauseArgs);
    }

    public AndConstraint<FakeBrokerAssertions> NotContainMessage(
        string topicName, string message,
        string because = "", params object[] becauseArgs)
    {
        var jsonMessage = JToken.Parse(message);
        return NotContainMessage(topicName, jsonMessage, because, becauseArgs);
    }

    public AndConstraint<FakeBrokerAssertions> NotContainMessage(
        string topicName, JToken message,
        string because = "", params object[] becauseArgs) =>
        ContainMessageAssert(topicName, message, not: true, because, becauseArgs);

    #endregion

    #region ContainsEquivalentMessage

    public AndConstraint<FakeBrokerAssertions> ContainsEquivalentMessage(
        string topicName, JToken message,
        string because = "", params object[] becauseArgs)
    {
        var itemOnArray = new JArray(message);
        Topic(topicName).And.ContainSubtree(itemOnArray, because, becauseArgs);
        return new AndConstraint<FakeBrokerAssertions>(this);
    }

    public AndConstraint<FakeBrokerAssertions> ContainsEquivalentMessage(
        string topicName, object message,
        string because = "", params object[] becauseArgs)
    {
        var jsonMessage = JsonSerializer.Serialize(message);
        return ContainsEquivalentMessage(topicName, jsonMessage, because, becauseArgs);
    }

    public AndConstraint<FakeBrokerAssertions> ContainsEquivalentMessage(
        string topicName, string message,
        string because = "", params object[] becauseArgs)
    {
        var jsonMessage = JToken.Parse(message);
        return ContainsEquivalentMessage(topicName, jsonMessage, because, becauseArgs);
    }

    #endregion

    #region HaveMessages

    AndConstraint<FakeBrokerAssertions> HaveMessagesAssert(
        object topicAndMessages, bool not,
        string because = "", params string[] becauseArgs)
    {
        var expected = JToken.Parse(JsonSerializer.Serialize(topicAndMessages));
        var pushedMessages = Subject.ProducedMessages();

        var received = DictToJToken(pushedMessages);

        if (not)
            received.Should().NotBeEquivalentTo(expected, because, becauseArgs);
        else
            received.Should().BeEquivalentTo(expected, because, becauseArgs);

        return new AndConstraint<FakeBrokerAssertions>(this);
    }

    public AndConstraint<FakeBrokerAssertions> HaveMessages(
        object topicAndMessages,
        string because = "", params string[] becauseArgs) =>
        HaveMessagesAssert(topicAndMessages, not: false, because, becauseArgs);

    public AndConstraint<FakeBrokerAssertions> HaveMessages(
        Dictionary<string, object[]> messages,
        string because = "", params string[] becauseArgs)
    {
        var strMessages = messages.ToDictionary(
            x => x.Key,
            x => x.Value.Select(v => JsonSerializer.Serialize(v)).ToArray());

        return HaveJsonMessages(strMessages, because, becauseArgs);
    }

    public AndConstraint<FakeBrokerAssertions> NotHaveMessages(
        Dictionary<string, object[]> messages,
        string because = "", params string[] becauseArgs)
    {
        var strMessages = messages.ToDictionary(
            x => x.Key,
            x => x.Value.Select(v => JsonSerializer.Serialize(v)).ToArray());

        return NotHaveJsonMessages(strMessages, because, becauseArgs);
    }

    public AndConstraint<FakeBrokerAssertions> NotHaveMessages(
        object topicAndMessages,
        string because = "", params string[] becauseArgs) =>
        HaveMessagesAssert(topicAndMessages, not: true, because, becauseArgs);

    AndConstraint<FakeBrokerAssertions> JsonMessagesBeAssertion(
        Dictionary<string, string[]> messages, bool not,
        string because = "",
        params string[] becauseArgs)
    {
        var pushedMessages = Subject.ProducedMessages();
        var expected = DictToJToken(messages);
        var received = DictToJToken(pushedMessages);

        if (not)
            received.Should().NotBeEquivalentTo(expected, because, becauseArgs);
        else
            received.Should().BeEquivalentTo(expected, because, becauseArgs);

        return new AndConstraint<FakeBrokerAssertions>(this);
    }

    public AndConstraint<FakeBrokerAssertions> HaveJsonMessages(
        Dictionary<string, string[]> messages,
        string because = "",
        params string[] becauseArgs) =>
        JsonMessagesBeAssertion(messages, not: false, because, becauseArgs);

    public AndConstraint<FakeBrokerAssertions> NotHaveJsonMessages(
        Dictionary<string, string[]> messages,
        string because = "",
        params string[] becauseArgs) =>
        JsonMessagesBeAssertion(messages, not: true, because, becauseArgs);

    #endregion

    #region ContainMessagesEquivalentTo

    public AndConstraint<FakeBrokerAssertions> ContainMessagesEquivalentTo(
        object topicAndMessages,
        string because = "", params string[] becauseArgs)
    {
        var expected = JToken.Parse(JsonSerializer.Serialize(topicAndMessages));
        var pushedMessages = Subject.ProducedMessages();

        var received = DictToJToken(pushedMessages);

        received.Should().ContainSubtree(expected, because, becauseArgs);
        return new AndConstraint<FakeBrokerAssertions>(this);
    }

    public AndConstraint<FakeBrokerAssertions> ContainMessagesEquivalentTo(
        Dictionary<string, object[]> messages,
        string because = "", params string[] becauseArgs)
    {
        var strMessages = messages.ToDictionary(
            x => x.Key,
            x => x.Value.Select(v => JsonSerializer.Serialize(v)).ToArray());

        return ContainJsonMessageSubtree(strMessages, because, becauseArgs);
    }

    public AndConstraint<FakeBrokerAssertions> ContainJsonMessageSubtree(
        Dictionary<string, string[]> messages,
        string because = "",
        params string[] becauseArgs)
    {
        var pushedMessages = Subject.ProducedMessages();
        var expected = DictToJToken(messages);
        var received = DictToJToken(pushedMessages);

        received.Should().ContainSubtree(expected, because, becauseArgs);

        return new AndConstraint<FakeBrokerAssertions>(this);
    }

    #endregion

    static JToken DictToJToken(IReadOnlyDictionary<string, string[]> values)
    {
        var root = new JObject();
        foreach (var (key, value) in values)
        {
            var items = new JArray(value.Select(JToken.Parse).ToArray());
            root.Add(key, items);
        }

        return root;
    }

    public async Task<FakeBrokerAssertions> When(Func<Task> action)
    {
        if (Subject is not IFakeBroker fake)
            throw new InvalidOperationException();

        var messages = await fake.Delta(action);
        var broker = new DeltaFakerBroker(messages);
        return new FakeBrokerAssertions(broker);
    }
}

class DeltaFakerBroker : IFakeReadonlyBroker
{
    readonly IReadOnlyDictionary<string, string[]> messages;

    public DeltaFakerBroker(IReadOnlyDictionary<string, string[]> messages) => this.messages = messages;
    public IReadOnlyDictionary<string, string[]> ProducedMessages() => messages;
    public string[] ProducedOn(string topic) => messages[topic];
}

public static class FakeBrokerAssertionsExtensions
{
    public static FakeBrokerAssertions Should(this IFakeBroker instance) => new(instance);
}
