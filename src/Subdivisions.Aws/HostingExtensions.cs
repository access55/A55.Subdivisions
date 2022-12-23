using Amazon.Runtime;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Subdivisions.Hosting;
using Subdivisions.Hosting.Config;
using Subdivisions.Hosting.Job;
using Subdivisions.Models;

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
            .AddScoped(typeof(IProducer<,>), typeof(TypedProducer<,>))
            .AddScoped(typeof(IProducer<,,>), typeof(TypedProducer<,,>))
            .AddSingleton<IConsumerFactory, ConsumerFactory>()
            .AddSingleton<IConsumerJob, ConcurrentConsumerJob>()
            .AddHostedService<SubdivisionsHostedService>();
    }

    public static void UseSubdivisions(this IEndpointRouteBuilder app)
    {
        var settings = app.ServiceProvider.GetService<IOptions<SubConfig>>();
        if (settings is null)
            throw new InvalidOperationException("You should call AddSubdivisions before");

        app.MapTopicEndpoints(settings.Value);
    }
}
