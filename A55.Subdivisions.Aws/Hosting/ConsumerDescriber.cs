namespace A55.Subdivisions.Aws.Hosting;

public interface IWeakConsumer
{
    internal Task Consume(object message, CancellationToken ctx);
}

public interface IConsumer<in TMessage> : IWeakConsumer where TMessage : notnull
{
    Task IWeakConsumer.Consume(object message, CancellationToken ctx) => Consume((TMessage)message, ctx);
    Task Consume(TMessage message, CancellationToken ctx);
}

public interface IConsumer : IConsumer<string>
{
}

interface IProducerDescriber
{
    string TopicName { get; }
}

interface IConsumerDescriber : IProducerDescriber
{
    TimeSpan? PollingInterval { get; }
    int? MaxConcurrency { get; }
    Type MessageType { get; }
    public Type ConsumerType { get; }
    Func<Exception, Task>? ErrorHandler { get; }
}

sealed class ConsumerDescriber : IConsumerDescriber
{
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

        if (consumerType is {IsAbstract: true, IsInterface: false})
            throw new SubdivisionsException($"Consumer should not be abstract: {consumerType.Name}");

        var consumerDef = consumerType.GetInterfaces().SingleOrDefault(i =>
            i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IConsumer<>));

        if (consumerDef is null || !consumerDef.GetGenericArguments().Single().IsAssignableFrom(messageType))
            throw new SubdivisionsException($"Invalid consumer message definition: {topicName}");

        MaxConcurrency = maxConcurrency;
        ConsumerType = consumerType;
        MessageType = messageType;
        ErrorHandler = errorHandler;
        PollingInterval = pollingInterval;
        TopicName = topicName;
    }

    public Type ConsumerType { get; }
    public Type MessageType { get; }
    public string TopicName { get; }
    public TimeSpan? PollingInterval { get; }
    public int? MaxConcurrency { get; }

    public Func<Exception, Task>? ErrorHandler { get; }
}
