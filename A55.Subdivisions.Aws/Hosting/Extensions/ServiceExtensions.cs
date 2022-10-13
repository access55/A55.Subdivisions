using A55.Subdivisions.Aws.Extensions;
using Amazon.Runtime;
using Microsoft.Extensions.DependencyInjection;

namespace A55.Subdivisions.Aws.Hosting.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddSubdivisions(
        this IServiceCollection services,
        Action<SubConfig>? config = null,
        AWSCredentials? credentials = null)
    {
        services.AddSubdivisionsClient(config, credentials);
        services.AddSubdivisionsHostedServices();
        return services;
    }

    public static TopicConfigurationBuilder<TMessage> MapTopic<TMessage>(
        this IServiceCollection services, string topicName)
        where TMessage : notnull
    {
        var builder = new TopicConfigurationBuilder<TMessage>(services, topicName);
        services.AddSingleton(sp => builder.CreateDescriber(sp));
        return builder;
    }

    public static IServiceCollection AddSubdivisions2(
        this IServiceCollection services,
        Action<SubConfigBuilder> config,
        AWSCredentials? credentials = null)
    {
        var builder = new SubConfigBuilder(services);
        config(builder);
        services.AddSubdivisionsClient(builder.Configure, credentials);
        services.AddSubdivisionsHostedServices();
        return services;
    }
}
