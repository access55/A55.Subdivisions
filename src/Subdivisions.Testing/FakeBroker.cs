using System.Collections.Immutable;
using MassTransit;
using Microsoft.Extensions.Options;
using Subdivisions.Clients;
using Subdivisions.Hosting;
using Subdivisions.Hosting.Job;
using Subdivisions.Models;
using Subdivisions.Services;

namespace Subdivisions.Testing;

public interface IFakeReadonlyBroker
{
    public IReadOnlyDictionary<string, string[]> ProducedMessages();

    public ISubMessageSerializer Serializer { get; }

    string[] GetConsumed(Type consumer, string topic);

    public string[] ProducedOn(string topic);
}

public interface IFakeBroker : IFakeReadonlyBroker
{
    Task<Guid> Produce(string topic, string message, Guid? correlationId = null, bool verifyOnly = false);

    Task<Guid> Produce<T>(string topic, T message, Guid? correlationId = null, bool verifyOnly = false)
        where T : notnull;

    void Reset();

    T[] ProducedOn<T>(string topic) where T : notnull;

    TMessage[] GetConsumed<TConsumer, TMessage>(string topic)
        where TMessage : notnull where TConsumer : IConsumer<TMessage>;

    ConsumedMessage[] GetConsumed(string topic);
    IDictionary<string, ConsumedMessage[]> GetConsumed();

    Task<IReadOnlyDictionary<string, string[]>> Delta(Func<Task> action);
    Task<string[]> Delta(string topic, Func<Task> action);
    Task<T[]> Delta<T>(string topic, Func<Task> action) where T : notnull;

    void AutoConsumeLoop(bool enabled = true);
}

public readonly record struct ConsumedMessage(Type Consumer, string Message);

class InMemoryBroker : IConsumeDriver, IProduceDriver, IConsumerJob, ISubResourceManager, IFakeBroker
{
    readonly ImmutableDictionary<string, IConsumerDescriber> consumers;

    public ISubMessageSerializer Serializer { get; }

    readonly IConsumerFactory consumerFactory;
    readonly SubConfig config;
    readonly ISubClock subClock;

    readonly Dictionary<string, List<IMessage<string>>> produced = new();
    readonly Dictionary<string, List<ConsumedMessage>> consumed = new();
    Dictionary<string, List<IMessage<string>>>? deltaMessages;

    bool autoConsumeLoop = false;

    public InMemoryBroker(
        IConsumerFactory consumerFactory,
        ISubMessageSerializer serializer,
        IEnumerable<IConsumerDescriber> describers,
        IOptions<SubConfig> config,
        ISubClock subClock)
    {
        this.consumerFactory = consumerFactory;
        this.Serializer = serializer;
        this.config = config.Value;
        this.subClock = subClock;

        this.consumers = describers.ToImmutableDictionary(
            x => x.TopicName, x => x);
    }

    const string OwnHeader = "[TEST_PUBLISH_MESSAGE]";

    public async Task<Guid> Produce<T>(string topic, T message, Guid? correlationId = null, bool verifyOnly = false)
        where T : notnull
    {
        var messageBody = Serializer.Serialize(message);
        return await Produce(topic, messageBody, correlationId, verifyOnly);
    }

    public async Task<Guid> Produce(string topic, string message, Guid? correlationId = null, bool verifyOnly = false)
    {
        var testMessage = verifyOnly ? message : $"{OwnHeader}{message}";
        var response = await Produce(new TopicId(topic, config), testMessage, correlationId, false, default);
        return response.MessageId;
    }

    public async Task<PublishResult> Produce(TopicId topic, string message, Guid? correlationId, bool compressed,
        CancellationToken ctx)
    {
        var owned = message.StartsWith(OwnHeader);
        if (owned)
            message = message[OwnHeader.Length..];

        var topicName = topic.RawName;
        var id = NewId.NextGuid();
        var sentMessage =
            new LocalMessage<string>(message) { MessageId = id, Datetime = subClock.Now(), RetryNumber = 0 };

        if (!produced.ContainsKey(topicName))
            produced.Add(topicName, new());
        produced[topicName].Add(sentMessage);

        if (deltaMessages is not null)
        {
            if (!deltaMessages.ContainsKey(topicName))
                deltaMessages.Add(topicName, new());
            deltaMessages[topicName].Add(sentMessage);
        }

        if (consumers.TryGetValue(topicName, out var describer))
        {
            if (autoConsumeLoop || owned)
                await consumerFactory.ConsumeScoped(describer, sentMessage, ctx);

            if (!consumed.TryGetValue(topicName, out var consumedMessages))
                consumed.Add(topicName, new());
            consumed[topicName].Add(new(describer.ConsumerType, message));
        }

        return new PublishResult(true, id, correlationId);
    }

    public void Reset()
    {
        produced.Clear();
        consumed.Clear();
        deltaMessages?.Clear();
        autoConsumeLoop = false;
    }

    static IReadOnlyDictionary<string, string[]> ExtractBody(Dictionary<string, List<IMessage<string>>> messages) =>
        messages.ToDictionary(x => x.Key, x => x.Value.Select(v => v.Body).ToArray());

    static string[] GetKeyOrEmpty(string key, IReadOnlyDictionary<string, string[]> dict) =>
        dict.TryGetValue(key, out var values) ? values : Array.Empty<string>();

    T[] Deserialize<T>(IEnumerable<string> bodies) where T : notnull =>
        bodies.Select(x => Serializer.Deserialize<T>(x)).ToArray();

    public IReadOnlyDictionary<string, string[]> ProducedMessages() => ExtractBody(produced);

    public string[] ProducedOn(string topic) => GetKeyOrEmpty(topic, ProducedMessages());

    public T[] ProducedOn<T>(string topic) where T : notnull =>
        Deserialize<T>(ProducedOn(topic));

    public ConsumedMessage[] GetConsumed(string topic) => consumed.TryGetValue(topic, out var consumedMessages)
        ? consumedMessages.ToArray()
        : Array.Empty<ConsumedMessage>();

    public IDictionary<string, ConsumedMessage[]> GetConsumed() =>
        consumed.ToImmutableDictionary(x => x.Key, x => x.Value.ToArray());

    public string[] GetConsumed(Type consumer, string topic) =>
        GetConsumed(topic).Where(x => x.Consumer == consumer).Select(x => x.Message).ToArray();

    public TMessage[] GetConsumed<TConsumer, TMessage>(string topic)
        where TConsumer : IConsumer<TMessage> where TMessage : notnull =>
        Deserialize<TMessage>(GetConsumed(typeof(TConsumer), topic));

    public async Task<IReadOnlyDictionary<string, string[]>> Delta(Func<Task> action)
    {
        deltaMessages = new();
        await action();
        var result = ExtractBody(deltaMessages);
        deltaMessages = null;
        return result;
    }

    public async Task<string[]> Delta(string topic, Func<Task> action) =>
        GetKeyOrEmpty(topic, await Delta(action));

    public async Task<T[]> Delta<T>(string topic, Func<Task> action) where T : notnull =>
        Deserialize<T>(await Delta(topic, action));

    public void AutoConsumeLoop(bool enabled = true) => autoConsumeLoop = enabled;

    public Task<IReadOnlyCollection<IMessage<string>>> ReceiveMessages(TopicId topic,
        CancellationToken ctx) =>
        Task.FromResult<IReadOnlyCollection<IMessage<string>>>(Array.Empty<IMessage<string>>());

    public Task<IReadOnlyCollection<IMessage<string>>> ReceiveDeadLetters(TopicId topic,
        CancellationToken ctx) =>
        Task.FromResult<IReadOnlyCollection<IMessage<string>>>(Array.Empty<IMessage<string>>());

    public Task Start(IReadOnlyCollection<IConsumerDescriber> describers, CancellationToken stoppingToken) =>
        Task.CompletedTask;

    public ValueTask EnsureQueueExists(string topic, CancellationToken ctx) => ValueTask.CompletedTask;
    public ValueTask EnsureTopicExists(string topic, CancellationToken ctx) => ValueTask.CompletedTask;
    public Task SetupLocalstack(CancellationToken ctx) => Task.CompletedTask;
}

class LocalMessage<T> : IMessage<T> where T : notnull
{
    public LocalMessage(T body) => Body = body;

    public Guid? MessageId { get; set; }
    public Guid? CorrelationId { get; set; }
    public DateTime Datetime { get; set; }
    public T Body { get; set; }
    public uint RetryNumber { get; set; }
    public string QueueUrl { get; set; } = "";
    public Task Delete() => Task.CompletedTask;
    public Task Release(TimeSpan delay) => Task.CompletedTask;

    public IMessage<TMap> Map<TMap>(Func<T, TMap> selector) where TMap : notnull =>
        new LocalMessage<TMap>(selector(Body))
        {
            MessageId = MessageId,
            Datetime = Datetime,
            CorrelationId = CorrelationId,
            RetryNumber = RetryNumber,
            QueueUrl = QueueUrl
        };
}
