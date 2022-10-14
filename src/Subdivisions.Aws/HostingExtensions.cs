using Amazon.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Subdivisions.Hosting;
using Subdivisions.Hosting.Config;
using Subdivisions.Hosting.Job;

namespace Subdivisions;

public static class HostingExtensions
{
    public static IServiceCollection AddSubdivisions(
        this IServiceCollection services,
        Action<SubConfigBuilder>? config = null,
        AWSCredentials? credentials = null)
    {
        var builder = new SubConfigBuilder(services);
        config?.Invoke(builder);
        return services
            .AddSubdivisionsServices(builder.Configure, credentials)
            .AddSingleton<IConsumerFactory, ConsumerFactory>()
            .AddSingleton<IConsumerJob, ConcurrentConsumerJob>()
            .AddHostedService<SubdivisionsHostedService>();
    }
}
