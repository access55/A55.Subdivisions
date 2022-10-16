using System.Collections.Immutable;
using Subdivisions.Clients;
using Subdivisions.Hosting;
using Subdivisions.Hosting.Job;
using Subdivisions.Models;
using Subdivisions.Services;

namespace Subdivisions.Testing;

public interface IFakeBroker
{
    Task<Guid> Publish(string topic, string message);
    void Reset();

    string[] ProducedOn(string topic);
    T[] ProducedOn<T>(string topic) where T : notnull;

    IReadOnlyDictionary<string, string[]> ProducedMessages();

    Task<IReadOnlyDictionary<string, string[]>> Delta(Func<Task> action);
    Task<string[]> Delta(string topic, Func<Task> action);
    Task<T[]> Delta<T>(string topic, Func<Task> action) where T : notnull;
}

class InMemoryClient : IConsumeDriver, IProduceDriver, IConsumerJob, ISubResourceManager, IFakeBroker
{
    readonly ImmutableDictionary<string, IConsumerDescriber> consumers;

    readonly IConsumerFactory consumerFactory;
    readonly ISubMessageSerializer serializer;
    readonly ISubClock subClock;

    readonly Dictionary<string, List<IMessage<string>>> produced = new();
    Dictionary<string, List<IMessage<string>>>? deltaMessages;

    public InMemoryClient(
        IConsumerFactory consumerFactory,
        ISubMessageSerializer serializer,
        IEnumerable<IConsumerDescriber> describers,
        ISubClock subClock)
    {
        this.consumerFactory = consumerFactory;
        this.serializer = serializer;
        this.subClock = subClock;

        this.consumers = describers.ToImmutableDictionary(
            x => x.TopicName, x => x);
    }

    public async Task<Guid> Publish(string topic, string message) => (await Produce(topic, message, default)).MessageId;

    public async Task<PublishResult> Produce(string topic, string message, CancellationToken ctx)
    {
        var id = Guid.NewGuid();
        var payload = new LocalMessage<string>(message) {MessageId = id, Datetime = subClock.Now(), RetryNumber = 0};

        if (!produced.ContainsKey(topic))
            produced.Add(topic, new());
        produced[topic].Add(payload);

        if (deltaMessages is not null)
        {
            if (!deltaMessages.ContainsKey(topic))
                deltaMessages.Add(topic, new());
            deltaMessages[topic].Add(payload);
        }

        if (consumers.TryGetValue(topic, out var describer))
            await consumerFactory.ConsumeScoped(describer, payload, ctx);

        return new PublishResult(true, id);
    }

    public void Reset()
    {
        produced.Clear();
        deltaMessages?.Clear();
    }

    static IReadOnlyDictionary<string, string[]> ExtractBody(Dictionary<string, List<IMessage<string>>> messages) =>
        messages.ToDictionary(x => x.Key, x => x.Value.Select(v => v.Body).ToArray());

    static string[] GetKeyOrEmpty(string key, IReadOnlyDictionary<string, string[]> dict) =>
        dict.TryGetValue(key, out var values) ? values : Array.Empty<string>();

    public T[] Deserialize<T>(IEnumerable<string> bodies) where T : notnull =>
        bodies.Select(x => serializer.Deserialize<T>(x)).ToArray();

    public IReadOnlyDictionary<string, string[]> ProducedMessages() => ExtractBody(produced);

    public string[] ProducedOn(string topic) => GetKeyOrEmpty(topic, ProducedMessages());

    public T[] ProducedOn<T>(string topic) where T : notnull =>
        Deserialize<T>(ProducedOn(topic));

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

    public Task<IReadOnlyCollection<IMessage<string>>> ReceiveMessages(string queue, CancellationToken ctx) =>
        Task.FromResult<IReadOnlyCollection<IMessage<string>>>(Array.Empty<IMessage<string>>());

    public Task<IReadOnlyCollection<IMessage<string>>> ReceiveDeadLetters(string queue, CancellationToken ctx) =>
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
    public DateTime Datetime { get; set; }
    public T Body { get; set; }
    public uint RetryNumber { get; set; }
    public Task Delete() => Task.CompletedTask;
    public Task Release(TimeSpan delay) => Task.CompletedTask;

    public IMessage<TMap> Map<TMap>(Func<T, TMap> selector) where TMap : notnull =>
        new LocalMessage<TMap>(selector(Body)) {MessageId = MessageId, Datetime = Datetime, RetryNumber = RetryNumber};
}
