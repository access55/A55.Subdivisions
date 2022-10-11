using A55.Subdivisions.Aws.Hosting;
using A55.Subdivisions.Aws.Models;
using Amazon.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace A55.Subdivisions.Aws.Extensions;

static class Extensions
{
    public static IServiceCollection AddAwsConfig<TConfig>(this IServiceCollection services)
        where TConfig : ClientConfig, new() =>
        services.AddTransient(sp =>
        {
            var subSettings = sp.GetRequiredService<IOptionsMonitor<SubConfig>>().CurrentValue;
            var config = new TConfig();
            if (!string.IsNullOrWhiteSpace(subSettings.ServiceUrl))
                config.ServiceURL = subSettings.ServiceUrl;
            else
                config.RegionEndpoint = subSettings.Endpoint;

            return config;
        });

    public static AWSCredentials GetAwsCredentials(this IServiceProvider provider) =>
        provider.GetRequiredService<AWSCredentials>();

    public static IServiceCollection AddSubdivisionsConsumer<TMessage, TConsumer>(
        this IServiceCollection services,
        string topic, int? maxConcurrent = null, TimeSpan? pollingInterval = null)
        where TMessage : notnull
        where TConsumer : class, IConsumer<TMessage>
        => services.AddSubdivisionsConsumer(
            new ConsumerDescriber(topic, typeof(TConsumer), typeof(TMessage), maxConcurrent, pollingInterval));

    public static IServiceCollection AddSubdivisionsConsumer(
        this IServiceCollection services,
        ConsumerDescriber consumer) =>
        services
            .AddSingleton<IConsumerDescriber>(consumer)
            .AddScoped(consumer.ConsumerType);
}
