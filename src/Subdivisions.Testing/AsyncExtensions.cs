using Newtonsoft.Json.Linq;

namespace Subdivisions.Testing;

public static class AsyncExtensions
{
    public static async Task HaveAnyMessage(
        this Task<FakeBrokerAssertions> @this,
        string topicName, string because = "", params object[] becauseArgs)
        => (await @this).HaveAnyMessage(topicName, because, becauseArgs);

    public static async Task Topic(
        this Task<FakeBrokerAssertions> @this,
        string topicName)
        => (await @this).Topic(topicName);

    public static async Task ContainMessage(
        this Task<FakeBrokerAssertions> @this,
        string topicName, object message,
        string because = "", params object[] becauseArgs)
        => (await @this).ContainsMessageOn(topicName, message, because, becauseArgs);

    public static async Task ContainMessage(
        this Task<FakeBrokerAssertions> @this,
        string topicName, string message,
        string because = "", params object[] becauseArgs)
        => (await @this).ContainsMessageOn(topicName, message, because, becauseArgs);

    public static async Task ContainMessage(
        this Task<FakeBrokerAssertions> @this,
        string topicName, JToken message,
        string because = "", params object[] becauseArgs)
        => (await @this).ContainsMessageOn(topicName, message, because, becauseArgs);

    public static async Task NotContainMessage(
        this Task<FakeBrokerAssertions> @this,
        string topicName, object message,
        string because = "", params object[] becauseArgs)
        => (await @this).NotContainsMessageOn(topicName, message, because, becauseArgs);

    public static async Task NotContainMessage(
        this Task<FakeBrokerAssertions> @this,
        string topicName, string message,
        string because = "", params object[] becauseArgs)
        => (await @this).NotContainsMessageOn(topicName, message, because, becauseArgs);

    public static async Task NotContainMessage(
        this Task<FakeBrokerAssertions> @this,
        string topicName, JToken message,
        string because = "", params object[] becauseArgs)
        => (await @this).NotContainsMessageOn(topicName, message, because, becauseArgs);

    public static async Task ContainsEquivalentMessage(
        this Task<FakeBrokerAssertions> @this,
        string topicName, JToken message,
        string because = "", params object[] becauseArgs)
        => (await @this).ContainsMessageEquivalentTo(topicName, message, because, becauseArgs);

    public static async Task ContainsEquivalentMessage(
        this Task<FakeBrokerAssertions> @this,
        string topicName, object message,
        string because = "", params object[] becauseArgs)
        => (await @this).ContainsMessageEquivalentTo(topicName, message, because, becauseArgs);

    public static async Task ContainsEquivalentMessage(
        this Task<FakeBrokerAssertions> @this,
        string topicName, string message,
        string because = "", params object[] becauseArgs)
        => (await @this).ContainsMessageEquivalentTo(topicName, message, because, becauseArgs);

    public static async Task MessagesBe(
        this Task<FakeBrokerAssertions> @this,
        object topicAndMessages,
        string because = "", params string[] becauseArgs)
        => (await @this).HaveReceived(topicAndMessages, because, becauseArgs);

    public static async Task MessagesBe(
        this Task<FakeBrokerAssertions> @this,
        Dictionary<string, object[]> messages,
        string because = "", params string[] becauseArgs)
        => (await @this).HaveReceived(messages, because, becauseArgs);

    public static async Task NotMessagesBe(
        this Task<FakeBrokerAssertions> @this,
        Dictionary<string, object[]> messages,
        string because = "", params string[] becauseArgs)
        => (await @this).NotHaveReceived(messages, because, becauseArgs);

    public static async Task NotMessagesBe(
        this Task<FakeBrokerAssertions> @this,
        object topicAndMessages,
        string because = "", params string[] becauseArgs)
        => (await @this).NotHaveReceived(topicAndMessages, because, becauseArgs);

    public static async Task JsonMessagesBe(
        this Task<FakeBrokerAssertions> @this,
        Dictionary<string, string[]> messages,
        string because = "",
        params string[] becauseArgs)
        => (await @this).HaveReceivedAsJson(messages, because, becauseArgs);

    public static async Task NotJsonMessagesBe(
        this Task<FakeBrokerAssertions> @this,
        Dictionary<string, string[]> messages,
        string because = "",
        params string[] becauseArgs)
        => (await @this).NotHaveReceivedAsJson(messages, because, becauseArgs);

    public static async Task BeMessagesEquivalent(
        this Task<FakeBrokerAssertions> @this,
        object topicAndMessages,
        string because = "", params string[] becauseArgs)
        => (await @this).HaveReceivedMessagesEquivalentTo(topicAndMessages, because, becauseArgs);

    public static async Task BeMessagesEquivalent(
        this Task<FakeBrokerAssertions> @this,
        Dictionary<string, object[]> messages,
        string because = "", params string[] becauseArgs)
        => (await @this).HaveReceivedMessagesEquivalentTo(messages, because, becauseArgs);

    public static async Task BeMessagesJsonEqual(
        this Task<FakeBrokerAssertions> @this,
        Dictionary<string, string[]> messages,
        string because = "",
        params string[] becauseArgs)
        => (await @this).HaveReceivedMessagesAsJsonSubtree(messages, because, becauseArgs);
}
