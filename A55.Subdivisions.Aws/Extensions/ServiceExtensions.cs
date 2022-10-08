using A55.Subdivisions.Aws.Adapters;
using A55.Subdivisions.Aws.Models;
using Amazon.EventBridge;
using Amazon.KeyManagementService;
using Amazon.Runtime;
using Amazon.SimpleNotificationService;
using Amazon.SQS;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace A55.Subdivisions.Aws.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddSubdivisionsClient(this IServiceCollection services,
        Action<SubConfig>? config = null,
        AWSCredentials? credentials = null)
    {
        services
            .Configure<SubConfig>(c => config?.Invoke(c));

        services
            .AddSingleton<ISubClock, UtcClock>()
            .AddSingleton<ISubMessageSerializer, SubJsonSerializer>();

        services
            .AddSingleton(credentials ?? FallbackCredentialsFactory.GetCredentials())
            .AddAwsConfig<AmazonSQSConfig>()
            .AddAwsConfig<AmazonEventBridgeConfig>()
            .AddAwsConfig<AmazonKeyManagementServiceConfig>()
            .AddAwsConfig<AmazonSimpleNotificationServiceConfig>();

        services.AddTransient<IAmazonSimpleNotificationService>(sp =>
        {
            var (cred, c) = sp.GetClientSettings<AmazonSimpleNotificationServiceConfig>();
            return new AmazonSimpleNotificationServiceClient(cred, c);
        });

        services.AddTransient<IAmazonSQS>(sp =>
        {
            var (cred, c) = sp.GetClientSettings<AmazonSQSConfig>();
            return new AmazonSQSClient(cred, c);
        });

        services.AddTransient<IAmazonEventBridge>(sp =>
        {
            var (cred, c) = sp.GetClientSettings<AmazonEventBridgeConfig>();
            return new AmazonEventBridgeClient(cred, c);
        });

        services.AddTransient<IAmazonKeyManagementService>(sp =>
        {
            var (cred, c) = sp.GetClientSettings<AmazonKeyManagementServiceConfig>();
            return new AmazonKeyManagementServiceClient(cred, c);
        });

        services.AddSingleton<AwsKms>();
        services.AddTransient<AwsEvents>();
        services.AddTransient<AwsSqs>();
        services.AddTransient<AwsSns>();
        services.AddTransient<AwsSubdivisionsBootstrapper>();
        services.AddTransient<AwsSubClient>();
        services.AddTransient<ISubClient>(sp => sp.GetRequiredService<AwsSubClient>());

        return services;
    }

    static (AWSCredentials, TConfig) GetClientSettings<TConfig>(this IServiceProvider sp) where TConfig : notnull
    {
        var cred = sp.GetRequiredService<AWSCredentials>();
        var c = sp.GetRequiredService<TConfig>();
        return (cred, c);
    }

    static IServiceCollection AddAwsConfig<TConfig>(this IServiceCollection services)
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
}
