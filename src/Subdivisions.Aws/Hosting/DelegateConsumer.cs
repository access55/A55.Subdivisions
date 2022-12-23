using Microsoft.Extensions.DependencyInjection;
using Subdivisions.Models;

namespace Subdivisions.Hosting;

sealed class DelegateConsumer<TMessage> : IMessageConsumer<TMessage> where TMessage : notnull
{
    readonly IServiceProvider provider;
    readonly Delegate handler;

    public DelegateConsumer(Delegate handler, IServiceProvider provider)
    {
        this.provider = provider;
        this.handler = handler;
        ValidateParams(handler);
    }

    public static (Type, Type[]) ValidateParams(Delegate handler)
    {
        var delegateType = handler.GetType();
        var funcParams = delegateType.GenericTypeArguments;
        if (!funcParams.Contains(typeof(TMessage)))
            throw new SubdivisionsException($"No parameter of type {typeof(TMessage).Name} found");
        return (delegateType, funcParams);
    }

    public async Task Consume(TMessage message, MessageMeta meta, CancellationToken ctx)
    {
        var (delegateType, funcParams) = ValidateParams(handler);

        if (delegateType.GetGenericTypeDefinition().FullName?.StartsWith("System.Func`") == true)
            funcParams = funcParams[..^1];

        await using var scope = provider.CreateAsyncScope();
        var args = funcParams
            .Select(t =>
                t switch
                {
                    _ when t == typeof(TMessage) => message,
                    _ when t == typeof(CancellationToken) => ctx,
                    _ when t == typeof(MessageMeta) => meta,
                    _ => scope.ServiceProvider.GetRequiredService(t),
                })
            .ToArray();

        if (this.handler.DynamicInvoke(args) is Task task)
            await task;
    }
}
