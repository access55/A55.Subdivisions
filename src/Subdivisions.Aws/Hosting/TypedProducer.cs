namespace Subdivisions.Hosting;

sealed class TypedProducer<TMessage> : IProducer<TMessage> where TMessage : notnull
{
    readonly IProducerClient producer;
    readonly string topicName;
    readonly bool useCompression;

    public TypedProducer(string topicName, bool useCompression, IProducerClient producer)
    {
        this.topicName = topicName;
        this.useCompression = useCompression;
        this.producer = producer;
    }

    public Task<PublishResult> Publish(TMessage message, Guid? correlationId = null, CancellationToken ctx = default) =>
        producer.Publish(topicName, message, correlationId, useCompression, ctx);
}

sealed class TypedProducer<TMessage1, TMessage2> : IProducer<TMessage1, TMessage2>
    where TMessage1 : notnull where TMessage2 : notnull
{
    readonly IProducer<TMessage1> producer1;
    readonly IProducer<TMessage2> producer2;

    public TypedProducer(IProducer<TMessage1> producer1, IProducer<TMessage2> producer2)
    {
        this.producer1 = producer1;
        this.producer2 = producer2;
    }

    public Task<PublishResult>
        Publish(TMessage1 message, Guid? correlationId = null, CancellationToken ctx = default) =>
        producer1.Publish(message, correlationId, ctx);

    public Task<PublishResult>
        Publish(TMessage2 message, Guid? correlationId = null, CancellationToken ctx = default) =>
        producer2.Publish(message, correlationId, ctx);
}

sealed class TypedProducer<TMessage1, TMessage2, TMessage3> : IProducer<TMessage1, TMessage2, TMessage3>
    where TMessage1 : notnull where TMessage2 : notnull where TMessage3 : notnull
{
    readonly IProducer<TMessage1> producer1;
    readonly IProducer<TMessage2> producer2;
    readonly IProducer<TMessage3> producer3;

    public TypedProducer(IProducer<TMessage1> producer1, IProducer<TMessage2> producer2, IProducer<TMessage3> producer3)
    {
        this.producer1 = producer1;
        this.producer2 = producer2;
        this.producer3 = producer3;
    }

    public Task<PublishResult>
        Publish(TMessage1 message, Guid? correlationId = null, CancellationToken ctx = default) =>
        producer1.Publish(message, correlationId, ctx);

    public Task<PublishResult>
        Publish(TMessage2 message, Guid? correlationId = null, CancellationToken ctx = default) =>
        producer2.Publish(message, correlationId, ctx);

    public Task<PublishResult>
        Publish(TMessage3 message, Guid? correlationId = null, CancellationToken ctx = default) =>
        producer3.Publish(message, correlationId, ctx);
}
