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
        => (await @this).ContainMessage(topicName, message, because, becauseArgs);

    public static async Task ContainMessage(
        this Task<FakeBrokerAssertions> @this,
        string topicName, string message,
        string because = "", params object[] becauseArgs)
        => (await @this).ContainMessage(topicName, message, because, becauseArgs);

    public static async Task ContainMessage(
        this Task<FakeBrokerAssertions> @this,
        string topicName, JToken message,
        string because = "", params object[] becauseArgs)
        => (await @this).ContainMessage(topicName, message, because, becauseArgs);

    public static async Task NotContainMessage(
        this Task<FakeBrokerAssertions> @this,
        string topicName, object message,
        string because = "", params object[] becauseArgs)
        => (await @this).NotContainMessage(topicName, message, because, becauseArgs);

    public static async Task NotContainMessage(
        this Task<FakeBrokerAssertions> @this,
        string topicName, string message,
        string because = "", params object[] becauseArgs)
        => (await @this).NotContainMessage(topicName, message, because, becauseArgs);

    public static async Task NotContainMessage(
        this Task<FakeBrokerAssertions> @this,
        string topicName, JToken message,
        string because = "", params object[] becauseArgs)
        => (await @this).NotContainMessage(topicName, message, because, becauseArgs);

    public static async Task ContainsEquivalentMessage(
        this Task<FakeBrokerAssertions> @this,
        string topicName, JToken message,
        string because = "", params object[] becauseArgs)
        => (await @this).ContainsEquivalentMessage(topicName, message, because, becauseArgs);

    public static async Task ContainsEquivalentMessage(
        this Task<FakeBrokerAssertions> @this,
        string topicName, object message,
        string because = "", params object[] becauseArgs)
        => (await @this).ContainsEquivalentMessage(topicName, message, because, becauseArgs);

    public static async Task ContainsEquivalentMessage(
        this Task<FakeBrokerAssertions> @this,
        string topicName, string message,
        string because = "", params object[] becauseArgs)
        => (await @this).ContainsEquivalentMessage(topicName, message, because, becauseArgs);

    public static async Task MessagesBe(
        this Task<FakeBrokerAssertions> @this,
        object topicAndMessages,
        string because = "", params string[] becauseArgs)
        => (await @this).MessagesBe(topicAndMessages, because, becauseArgs);

    public static async Task MessagesBe(
        this Task<FakeBrokerAssertions> @this,
        Dictionary<string, object[]> messages,
        string because = "", params string[] becauseArgs)
        => (await @this).MessagesBe(messages, because, becauseArgs);

    public static async Task NotMessagesBe(
        this Task<FakeBrokerAssertions> @this,
        Dictionary<string, object[]> messages,
        string because = "", params string[] becauseArgs)
        => (await @this).NotMessagesBe(messages, because, becauseArgs);

    public static async Task NotMessagesBe(
        this Task<FakeBrokerAssertions> @this,
        object topicAndMessages,
        string because = "", params string[] becauseArgs)
        => (await @this).NotMessagesBe(topicAndMessages, because, becauseArgs);

    public static async Task JsonMessagesBe(
        this Task<FakeBrokerAssertions> @this,
        Dictionary<string, string[]> messages,
        string because = "",
        params string[] becauseArgs)
        => (await @this).JsonMessagesBe(messages, because, becauseArgs);

    public static async Task NotJsonMessagesBe(
        this Task<FakeBrokerAssertions> @this,
        Dictionary<string, string[]> messages,
        string because = "",
        params string[] becauseArgs)
        => (await @this).NotJsonMessagesBe(messages, because, becauseArgs);

    public static async Task BeMessagesEquivalent(
        this Task<FakeBrokerAssertions> @this,
        object topicAndMessages,
        string because = "", params string[] becauseArgs)
        => (await @this).BeMessagesEquivalent(topicAndMessages, because, becauseArgs);

    public static async Task BeMessagesEquivalent(
        this Task<FakeBrokerAssertions> @this,
        Dictionary<string, object[]> messages,
        string because = "", params string[] becauseArgs)
        => (await @this).BeMessagesEquivalent(messages, because, becauseArgs);

    public static async Task BeMessagesJsonEqual(
        this Task<FakeBrokerAssertions> @this,
        Dictionary<string, string[]> messages,
        string because = "",
        params string[] becauseArgs)
        => (await @this).BeMessagesJsonEqual(messages, because, becauseArgs);
}
