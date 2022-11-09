using Subdivisions.Models;

namespace Subdivisions.Hosting;

interface IConsumerDescriber
{
    string TopicName { get; }
    TimeSpan PollingInterval { get; }
    int MaxConcurrency { get; }
    Type MessageType { get; }
    public Type ConsumerType { get; }
    Func<Exception, Task>? ErrorHandler { get; }
}

sealed class ConsumerConfig
{
    public int MaxConcurrency { get; set; }
    public bool UseCompression { get; set; }
    public TimeSpan PollingInterval { get; set; }
    public Func<Exception, Task>? ErrorHandler { get; set; }
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

        if (!Models.TopicId.IsValidTopicName(topicName))
            throw new SubdivisionsException($"Invalid topic names: {topicName}");

        if (!consumerType.IsAssignableTo(typeof(IWeakConsumer)))
            throw new SubdivisionsException($"Invalid consumer type: {consumerType.Name}");

        if (consumerType is { IsAbstract: true, IsInterface: false })
            throw new SubdivisionsException($"Consumer should not be abstract: {consumerType.Name}");

        var consumerDef = consumerType.GetInterfaces().SingleOrDefault(i =>
            i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IConsumer<>));

        if (consumerDef is null || !consumerDef.GetGenericArguments().Single().IsAssignableFrom(messageType))
            throw new SubdivisionsException($"Invalid consumer message definition: {topicName}");

        TopicName = topicName;
        MessageType = messageType;
        ConsumerType = consumerType;

        ErrorHandler = config.ErrorHandler;
        PollingInterval = config.PollingInterval;
        MaxConcurrency = config.MaxConcurrency;
    }

    public Type ConsumerType { get; }
    public Type MessageType { get; }
    public string TopicName { get; }
    public TimeSpan PollingInterval { get; }
    public int MaxConcurrency { get; }

    public Func<Exception, Task>? ErrorHandler { get; }
}
