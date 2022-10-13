using A55.Subdivisions.Clients;
using A55.Subdivisions.Extensions;
using A55.Subdivisions.Models;
using A55.Subdivisions.Services;
using Amazon.EventBridge;
using Amazon.KeyManagementService;
using Amazon.Runtime;
using Amazon.SimpleNotificationService;
using Amazon.SQS;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace A55.Subdivisions;

public static class ServicesExtensions
{
    public static IServiceCollection AddSubdivisionsServices(
        this IServiceCollection services,
        Action<SubConfig>? config = null,
        AWSCredentials? credentials = null)
    {
        services
            .AddSingleton<IConfigureOptions<SubConfig>, ConfigureSubConfigOptions>()
            .PostConfigure<SubConfig>(c => config?.Invoke(c));

        services
            .AddSingleton(sp =>
            {
                if (credentials is not null)
                    return credentials;
                if (sp.GetRequiredService<IOptions<SubConfig>>().Value.Localstack)
                    return new AnonymousAWSCredentials();
                return FallbackCredentialsFactory.GetCredentials();
            });

        services
            .AddAwsConfig<AmazonSQSConfig>()
            .AddAwsConfig<AmazonEventBridgeConfig>()
            .AddAwsConfig<AmazonKeyManagementServiceConfig>()
            .AddAwsConfig<AmazonSimpleNotificationServiceConfig>();

        services
            .AddTransient<IAmazonSimpleNotificationService>(sp =>
                new AmazonSimpleNotificationServiceClient(
                    sp.GetAwsCredentials(), sp.GetRequiredService<AmazonSimpleNotificationServiceConfig>()))
            .AddTransient<IAmazonSQS>(sp =>
                new AmazonSQSClient(sp.GetAwsCredentials(), sp.GetRequiredService<AmazonSQSConfig>()))
            .AddTransient<IAmazonEventBridge>(sp =>
                new AmazonEventBridgeClient(sp.GetAwsCredentials(), sp.GetRequiredService<AmazonEventBridgeConfig>()))
            .AddTransient<IAmazonKeyManagementService>(sp =>
                new AmazonKeyManagementServiceClient(sp.GetAwsCredentials(),
                    sp.GetRequiredService<AmazonKeyManagementServiceConfig>()));

        services
            .AddSingleton<AwsKms>()
            .AddTransient<AwsEvents>()
            .AddTransient<AwsSqs>()
            .AddTransient<AwsSns>();

        services
            .AddSingleton<ISubClock, UtcClock>()
            .AddSingleton<IRetryStrategy, Power2RetryStrategy>()
            .AddSingleton<ISubMessageSerializer, SubJsonSerializer>()
            .AddTransient<ISubResourceManager, AwsResourceManager>()
            .AddTransient<ISubdivisionsClient, AwsSubClient>()
            .AddTransient<IProducerClient>(sp => sp.GetRequiredService<ISubdivisionsClient>())
            .AddTransient<IConsumerClient>(sp => sp.GetRequiredService<ISubdivisionsClient>());

        return services;
    }
}
