using A55.Subdivisions.Aws.Models;

namespace A55.Subdivisions.Aws.Hosting;

public interface IWeakConsumer
{
    internal Task Consume(object message, CancellationToken ctx);
}

public interface IConsumer<in TMessage> : IWeakConsumer where TMessage : notnull
{
    Task Consume(TMessage message, CancellationToken ctx);

    Task IWeakConsumer.Consume(object message, CancellationToken ctx) => Consume((TMessage)message, ctx);
}

public interface IConsumer : IConsumer<string>
{
}

interface IConsumerDescriber
{
    string TopicName { get; }
    TimeSpan? PollingInterval { get; }
    int? MaxConcurrency { get; }
    Type MessageType { get; }
    public Type ConsumerType { get; }
    Func<Exception, Task>? ErrorHandler { get; }
}

sealed class ConsumerDescriber : IConsumerDescriber
{
    public Type ConsumerType { get; }
    public Type MessageType { get; }
    public string TopicName { get; }
    public TimeSpan? PollingInterval { get; }
    public int? MaxConcurrency { get; }

    public Func<Exception, Task>? ErrorHandler { get; }

    public ConsumerDescriber(
        string topicName,
        Type consumerType,
        Type messageType,
        int? maxConcurrency = null,
        TimeSpan? pollingInterval = null,
        Func<Exception, Task>? errorHandler = null
    )
    {
        ArgumentNullException.ThrowIfNull(topicName);
        ArgumentNullException.ThrowIfNull(consumerType);
        ArgumentNullException.ThrowIfNull(messageType);

        if (!Models.TopicName.IsValidTopicName(topicName))
            throw new SubdivisionsException($"Invalid topic names: {topicName}");

        if (!consumerType.IsAssignableTo(typeof(IWeakConsumer)))
            throw new SubdivisionsException($"Invalid consumer type: {consumerType.Name}");

        if (consumerType.GetGenericTypeDefinition() == typeof(IConsumer<>) &&
            consumerType.GetGenericArguments().Single().IsAssignableTo(messageType))
            throw new SubdivisionsException($"Invalid consumer message definition: {topicName}");

        MaxConcurrency = maxConcurrency;
        ConsumerType = consumerType;
        MessageType = messageType;
        ErrorHandler = errorHandler;
        PollingInterval = pollingInterval;
        TopicName = topicName;
    }
}
