using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Subdivisions.Clients;
using Subdivisions.Hosting.Job;
using Subdivisions.Models;
using Subdivisions.Services;

namespace Subdivisions.Testing;

public static class Extensions
{
    public static IServiceCollection MockSubdivisions(this IServiceCollection services) =>
        services
            .PostConfigure<SubConfig>(config =>
            {
                config.MessageTimeoutInSeconds = int.MaxValue;
                config.LongPollingWaitInSeconds = 0;
                config.RethrowExceptions = true;
            })
            .RemoveAll<IConsumeDriver>()
            .RemoveAll<IProduceDriver>()
            .RemoveAll<IConsumerJob>()
            .RemoveAll<ISubResourceManager>()
            .AddSingleton<InMemoryBroker>()
            .AddSingleton<IFakeBroker>(sp => sp.GetRequiredService<InMemoryBroker>())
            .AddSingleton<IConsumeDriver>(sp => sp.GetRequiredService<InMemoryBroker>())
            .AddSingleton<IProduceDriver>(sp => sp.GetRequiredService<InMemoryBroker>())
            .AddSingleton<IConsumerJob>(sp => sp.GetRequiredService<InMemoryBroker>())
            .AddSingleton<ISubResourceManager>(sp => sp.GetRequiredService<InMemoryBroker>());
}
