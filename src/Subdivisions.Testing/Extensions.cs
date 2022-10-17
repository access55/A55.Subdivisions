using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Subdivisions.Clients;
using Subdivisions.Hosting.Job;
using Subdivisions.Services;

namespace Subdivisions.Testing;

public static class Extensions
{
    public static IServiceCollection MockSubdivisions(this IServiceCollection services) =>
        services
            .RemoveAll<IConsumeDriver>()
            .RemoveAll<IProduceDriver>()
            .RemoveAll<IConsumerJob>()
            .RemoveAll<ISubResourceManager>()
            .AddSingleton<InMemoryClient>()
            .AddSingleton<IFakeBroker>(sp => sp.GetRequiredService<InMemoryClient>())
            .AddSingleton<IConsumeDriver>(sp => sp.GetRequiredService<InMemoryClient>())
            .AddSingleton<IProduceDriver>(sp => sp.GetRequiredService<InMemoryClient>())
            .AddSingleton<IConsumerJob>(sp => sp.GetRequiredService<InMemoryClient>())
            .AddSingleton<ISubResourceManager>(sp => sp.GetRequiredService<InMemoryClient>());
}
