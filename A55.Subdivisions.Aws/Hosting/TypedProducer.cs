namespace A55.Subdivisions.Hosting;

class TypedProducer<TMessage> : IProducer<TMessage> where TMessage : notnull
{
    readonly string topicName;
    readonly IProducerClient producer;

    public TypedProducer(string topicName, IProducerClient producer)
    {
        this.topicName = topicName;
        this.producer = producer;
    }

    public Task<PublishResult> Publish(TMessage message) => producer.Publish(topicName, message);
}
