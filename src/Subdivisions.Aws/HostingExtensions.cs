using Amazon.Runtime;
using CorrelationId;
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
        builder.ConfigureServices();
        return services
            .AddSubdivisionsServices(builder.ConfigureOptions, credentials)
            .AddScoped<ISubCorrelationIdContext, SubCorrelationIdContext>()
            .AddSingleton<IConsumerFactory, ConsumerFactory>()
            .AddSingleton<IConsumerJob, ConcurrentConsumerJob>()
            .AddHostedService<SubdivisionsHostedService>();
    }
}
