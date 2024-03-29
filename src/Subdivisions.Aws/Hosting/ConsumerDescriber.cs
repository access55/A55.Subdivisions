using Subdivisions.Models;

namespace Subdivisions.Hosting;

interface IConsumerDescriber
{
    string TopicName { get; }
    TimeSpan PollingInterval { get; }
    int MaxConcurrency { get; }
    TimeSpan ConsumeTimeout { get; }
    Type MessageType { get; }
    public Type ConsumerType { get; }
    Func<Exception, Task>? ErrorListener { get; }
    TopicNameOverride? NameOverride { get; set; }
}

sealed class ConsumerConfig
{
    public int MaxConcurrency { get; set; }
    public TimeSpan ConsumeTimeout { get; set; }
    public TimeSpan PollingInterval { get; set; }
    public Func<Exception, Task>? ErrorHandler { get; set; }
    public TopicNameOverride? NameOverride { get; set; }
}

sealed class ConsumerDescriber : IConsumerDescriber
{
    public ConsumerDescriber(
        string topicName,
        Type consumerType,
        Type messageType,
        ConsumerConfig? config = null
    )
    {
        ArgumentNullException.ThrowIfNull(topicName);
        ArgumentNullException.ThrowIfNull(consumerType);
        ArgumentNullException.ThrowIfNull(messageType);
        config ??= new ConsumerConfig();

        if (!TopicId.IsValidTopicName(topicName))
            throw new SubdivisionsException($"Invalid topic names: {topicName}");

        if (!consumerType.IsAssignableTo(typeof(IWeakConsumer)))
            throw new SubdivisionsException(
                $"Invalid consumer type: {consumerType.Name}");

        if (consumerType is { IsAbstract: true, IsInterface: false })
            throw new SubdivisionsException(
                $"Consumer should not be abstract: {consumerType.Name}");

        var interfaces = consumerType.GetInterfaces().Where(i => i.IsGenericType).ToArray();
        var consumerDef =
            interfaces.SingleOrDefault(i =>
                i.GetGenericTypeDefinition() == typeof(IConsumer<>))
            ??
            interfaces.SingleOrDefault(i =>
                i.GetGenericTypeDefinition() == typeof(IMessageConsumer<>));

        if (consumerDef?.GetGenericArguments().FirstOrDefault() is not { } consumerMessageType)
            throw new SubdivisionsException(
                $"Invalid consumer message definition: {topicName}");

        if (!consumerMessageType.IsAssignableFrom(messageType))
            throw new SubdivisionsException(
                $"{topicName} Configuration: Consumer message type {consumerMessageType.Name} don't match with topic message type {messageType.Name}");

        TopicName = topicName;
        MessageType = messageType;
        ConsumerType = consumerType;
        ErrorListener = config.ErrorHandler;
        PollingInterval = config.PollingInterval;
        ConsumeTimeout = config.ConsumeTimeout;
        MaxConcurrency = config.MaxConcurrency;
        NameOverride = config.NameOverride;
    }

    public Type ConsumerType { get; }
    public Type MessageType { get; }
    public string TopicName { get; }
    public TimeSpan PollingInterval { get; }
    public int MaxConcurrency { get; }
    public TimeSpan ConsumeTimeout { get; }
    public TopicNameOverride? NameOverride { get; set; }
    public Func<Exception, Task>? ErrorListener { get; }
}
