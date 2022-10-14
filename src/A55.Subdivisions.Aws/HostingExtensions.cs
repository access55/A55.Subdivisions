using A55.Subdivisions.Hosting;
using A55.Subdivisions.Hosting.Config;
using A55.Subdivisions.Hosting.Job;
using Amazon.Runtime;
using Microsoft.Extensions.DependencyInjection;

namespace A55.Subdivisions;

public static class HostingExtensions
{
    internal static IServiceCollection AddSubdivisionsHostedServices(this IServiceCollection services) =>
        services
            .AddSingleton<IConsumerFactory, ConsumerFactory>()
            .AddSingleton<IConsumerJob, ConcurrentConsumerJob>()
            .AddHostedService<SubdivisionsHostedService>();

    public static IServiceCollection AddSubdivisions(
        this IServiceCollection services,
        Action<SubConfigBuilder>? config = null,
        AWSCredentials? credentials = null)
    {
        var builder = new SubConfigBuilder(services);
        config?.Invoke(builder);
        services.AddSubdivisionsServices(builder.Configure, credentials);
        services.AddSubdivisionsHostedServices();
        return services;
    }
}
