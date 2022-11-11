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
    public Task<PublishResult> Publish(TMessage message, Guid? correlationId = null, CancellationToken ctx = default);
}

public interface IProducer<TMessage1, TMessage2> where TMessage1 : notnull where TMessage2 : notnull
{
    public Task<PublishResult> Publish(TMessage1 message, Guid? correlationId = null, CancellationToken ctx = default);
    public Task<PublishResult> Publish(TMessage2 message, Guid? correlationId = null, CancellationToken ctx = default);
}

public interface IProducer<TMessage1, TMessage2, TMessage3>
    where TMessage1 : notnull
    where TMessage2 : notnull
    where TMessage3 : notnull
{
    public Task<PublishResult> Publish(TMessage1 message, Guid? correlationId = null, CancellationToken ctx = default);
    public Task<PublishResult> Publish(TMessage2 message, Guid? correlationId = null, CancellationToken ctx = default);
    public Task<PublishResult> Publish(TMessage3 message, Guid? correlationId = null, CancellationToken ctx = default);
}
