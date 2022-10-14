namespace Subdivisions;

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

public interface IProducer<TMessage> where TMessage : notnull
{
    public Task<PublishResult> Publish(TMessage message, CancellationToken ctx = default);
}
