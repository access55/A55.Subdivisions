using A55.Subdivisions.Models;
using Microsoft.Extensions.DependencyInjection;

namespace A55.Subdivisions.Hosting.Job;

sealed class ConsumerDelegate<TMessage> : IConsumer<TMessage> where TMessage : notnull
{
    readonly IServiceProvider provider;
    readonly Delegate handler;

    public ConsumerDelegate(Delegate handler, IServiceProvider provider)
    {
        this.provider = provider;
        this.handler = handler;
    }

    public async Task Consume(TMessage message, CancellationToken ctx)
    {
        var funcParams = handler.GetType().GenericTypeArguments;
        if (!funcParams.Contains(typeof(TMessage)))
            throw new SubdivisionsException($"No parameter of type {typeof(TMessage).Name} found");

        await using var scope = provider.CreateAsyncScope();
        var args = funcParams
            .Select(t =>
                t switch
                {
                    _ when t == typeof(TMessage) => message,
                    _ when t == typeof(CancellationToken) => ctx,
                    _ => scope.ServiceProvider.GetRequiredService(t),
                })
            .ToArray();

        if (this.handler.DynamicInvoke(args) is Task task)
            await task;
    }
}
