namespace Subdivisions.Hosting;

class TypedProducer<TMessage> : IProducer<TMessage> where TMessage : notnull
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
