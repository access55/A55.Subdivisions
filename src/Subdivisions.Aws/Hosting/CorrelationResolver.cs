using CorrelationId;
using CorrelationId.Abstractions;
using MassTransit;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Subdivisions.Hosting;

class NewIdCorrelationIdProvider : ICorrelationIdProvider
{
    public string GenerateCorrelationId(HttpContext ctx) => NewId.NextGuid().ToString();
}

interface ICorrelationResolver
{
    Guid? GetId();
}

class CorrelationResolver : ICorrelationResolver
{
    readonly Guid? correlationId;

    public CorrelationResolver(ICorrelationContextAccessor? contextAccessor = null) =>
        correlationId =
            Guid.TryParse(contextAccessor?.CorrelationContext.CorrelationId, out var parsedId)
                ? parsedId
                : null;

    public Guid? GetId() => correlationId;
}

interface ISubCorrelationIdContext : IDisposable
{
    void Create(Guid? id);
}

sealed class SubCorrelationIdContext : ISubCorrelationIdContext
{
    readonly ICorrelationContextFactory contextFactory;
    readonly IOptions<CorrelationIdOptions> options;

    public SubCorrelationIdContext(
        ICorrelationContextFactory contextFactory,
        IOptions<CorrelationIdOptions> options
    )
    {
        this.contextFactory = contextFactory;
        this.options = options;
    }

    public void Create(Guid? id)
    {
        var correlationId = id ?? NewId.NextGuid();
        contextFactory.Create(correlationId.ToString(), options.Value.RequestHeader);
    }

    public void Dispose() => contextFactory.Dispose();
}
