using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Reflection;
using System.Text;
using Microsoft.Extensions.Options;
using Subdivisions.Models;

namespace Subdivisions;

interface IDiagnostics
{
    void SetActivityMessageAttributes(Activity? activity,
        string url,
        Guid? messageId,
        Guid? correlationId,
        string rawMessage,
        string compressedMessage
    );

    Activity? StartProducerActivity(string topic);
    Activity? StartConsumerActivity(string topic);
    Activity? StartProcessActivity(string topic);
    void AddRetrievedMessages(long quantity);
    void AddConsumedMessagesCounter(long quantity, TimeSpan duration);
    void AddProducedMessagesCounter(long quantity);
    void AddFailedMessagesCounter(long quantity, TimeSpan duration);
}

class Diagnostics : IDiagnostics
{
    readonly SubConfig config;

    const string SourceName = "A55.Subdivisions";

    static readonly ActivitySource activitySource =
        new(SourceName, Assembly.GetExecutingAssembly().GetName().Version?.ToString());

    static readonly Meter meterSource =
        new(SourceName, Assembly.GetExecutingAssembly().GetName().Version?.ToString());

    static readonly Counter<long> retrievedMessagesCounter = meterSource.CreateCounter<long>("RetrievedMessages");
    static readonly Counter<long> consumedMessagesCounter = meterSource.CreateCounter<long>("ConsumedMessages");
    static readonly Counter<long> producedMessagesCounter = meterSource.CreateCounter<long>("ProducedMessages");
    static readonly Counter<long> failedMessagesCounter = meterSource.CreateCounter<long>("FailedMessages");

    public Diagnostics(IOptions<SubConfig> config) => this.config = config.Value;

    public void SetActivityMessageAttributes(Activity? activity,
        string url,
        Guid? messageId,
        Guid? correlationId,
        string rawMessage,
        string compressedMessage
    )
    {
        var attr = new Dictionary<string, object>()
        {
            ["messaging.url"] = url,
            ["messaging.message_id"] = messageId ?? Guid.Empty,
            ["messaging.conversation_id"] = correlationId ?? Guid.Empty,
            ["messaging.correlation_id"] = correlationId ?? Guid.Empty,
            ["messaging.message_payload_size_bytes"] = Encoding.UTF8.GetByteCount(compressedMessage),
            ["messaging.message_payload_compressed_size_bytes"] = Encoding.UTF8.GetByteCount(rawMessage)
        };
        foreach (var (key, value) in attr)
            activity?.SetTag(key, value);
    }

    public Activity? StartProducerActivity(string topic) =>
        StartActivity(topic, ActivityKind.Producer, "send");

    public Activity? StartConsumerActivity(string topic)
    {
        const string operation = "receive";
        var activity = StartActivity(topic, ActivityKind.Consumer, operation);
        activity?.SetTag("messaging.operation", operation);
        activity?.SetTag("messaging.consumer_id", config.Source);
        return activity;
    }

    public Activity? StartProcessActivity(string topic)
    {
        const string operation = "process";
        var activity = StartActivity(topic, ActivityKind.Consumer, operation);
        activity?.SetTag("messaging.operation", operation);
        activity?.SetTag("messaging.consumer_id", config.Source);
        return activity;
    }

    static Activity? StartActivity(string topic, ActivityKind kind, string operation)
    {
        var activity = activitySource.StartActivity($"{topic} {operation}", kind);
        activity?.SetTag("messaging.destination_kind", "topic");
        activity?.SetTag("messaging.system", "AmazonSQS");
        activity?.SetTag("messaging.protocol", "AMQP");
        activity?.SetTag("messaging.protocol_version", "");
        activity?.SetTag("messaging.destination", topic);

        return activity;
    }

    public void AddRetrievedMessages(long quantity) => retrievedMessagesCounter.Add(quantity);

    public void AddConsumedMessagesCounter(long quantity, TimeSpan duration) =>
        consumedMessagesCounter.Add(quantity,
            new KeyValuePair<string, object?>("duration", duration.TotalMilliseconds));

    public void AddProducedMessagesCounter(long quantity) => producedMessagesCounter.Add(quantity);

    public void AddFailedMessagesCounter(long quantity, TimeSpan duration) => failedMessagesCounter.Add(quantity,
        new KeyValuePair<string, object?>("duration", duration.TotalMilliseconds));
}
