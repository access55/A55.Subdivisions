using System.Diagnostics;
using System.Reflection;
using System.Text;
using Microsoft.Extensions.Options;
using Subdivisions.Models;

namespace Subdivisions;

enum DestinationKind
{
    Queue,
    Topic
}

interface IDiagnostics
{
    void SetActivityMessageAttributes(Activity? activity,
        string url,
        IMessage message
    );

    Activity? StartProducerActivity(TopicId topic);
    Activity? StartConsumerActivity(TopicId topic);
    Activity? StartProcessActivity(TopicId topic);
}

class Diagnostics : IDiagnostics
{
    readonly SubConfig config;

    public static readonly ActivitySource ActivitySource = new(
        "A55.Subdivisions", Assembly.GetExecutingAssembly().GetName().Version?.ToString());

    public Diagnostics(IOptions<SubConfig> config) => this.config = config.Value;

    public void SetActivityMessageAttributes(Activity? activity,
        string url,
        IMessage message
    )
    {
        var messageSize = Encoding.UTF8.GetByteCount(message.Body);
        var attr = new Dictionary<string, object>()
        {
            ["messaging.url"] = url,
            ["messaging.message_id"] = message.MessageId ?? Guid.Empty,
            ["messaging.conversation_id"] = message.CorrelationId ?? Guid.Empty,
            ["messaging.correlation_id"] = message.CorrelationId ?? Guid.Empty,
            ["messaging.message_payload_size_bytes"] = messageSize,
            ["messaging.message_payload_compressed_size_bytes"] = messageSize
        };
        foreach (var (key, value) in attr)
            activity?.SetTag(key, value);
    }

    public Activity? StartProducerActivity(TopicId topic) =>
        StartActivity(topic, DestinationKind.Topic, "send");

    public Activity? StartConsumerActivity(TopicId topic)
    {
        const string operation = "receive";
        var activity = StartActivity(topic, DestinationKind.Queue, operation);
        activity?.SetTag("messaging.operation", operation);
        activity?.SetTag("messaging.consumer_id", config.Source);
        return activity;
    }

    public Activity? StartProcessActivity(TopicId topic)
    {
        const string operation = "process";
        var activity = StartActivity(topic, DestinationKind.Queue, operation);
        activity?.SetTag("messaging.operation", operation);
        activity?.SetTag("messaging.consumer_id", config.Source);
        return activity;
    }

    Activity? StartActivity(TopicId topic, DestinationKind kind, string operation)
    {
        var activity = ActivitySource.StartActivity($"{topic.TopicName} {operation}");
        activity?.SetTag("messaging.destination_kind", kind.ToString().ToLowerInvariant());
        activity?.SetTag("messaging.system", "AmazonSQS");
        activity?.SetTag("messaging.protocol", "AMQP");
        activity?.SetTag("messaging.protocol_version", "");
        activity?.SetTag("messaging.destination", topic.TopicName);

        return activity;
    }
}
