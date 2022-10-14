namespace Subdivisions.Hosting;

class TypedProducer<TMessage> : IProducer<TMessage> where TMessage : notnull
{
    readonly IProducerClient producer;
    readonly string topicName;

    public TypedProducer(string topicName, IProducerClient producer)
    {
        this.topicName = topicName;
        this.producer = producer;
    }

    public Task<PublishResult> Publish(TMessage message, CancellationToken ctx = default) =>
        producer.Publish(topicName, message, ctx);
}
