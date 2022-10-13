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

    public static TopicConfigurationBuilder<TMessage> MapTopic<TMessage>(
        this IServiceCollection services, string topicName)
        where TMessage : notnull
    {
        var builder = new TopicConfigurationBuilder<TMessage>(services, topicName);
        services.AddSingleton(sp => builder.CreateDescriber(sp));
        return builder;
    }

    public static IServiceCollection AddSubdivisions(
        this IServiceCollection services,
        Action<SubConfigBuilder> config,
        AWSCredentials? credentials = null)
    {
        var builder = new SubConfigBuilder(services);
        config(builder);
        services.AddSubdivisionsServices(builder.Configure, credentials);
        services.AddSubdivisionsHostedServices();
        return services;
    }
}
