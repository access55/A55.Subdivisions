using A55.Subdivisions.Aws.Clients;
using A55.Subdivisions.Aws.Hosting;
using A55.Subdivisions.Aws.Hosting.Job;
using Amazon.EventBridge;
using Amazon.KeyManagementService;
using Amazon.Runtime;
using Amazon.SimpleNotificationService;
using Amazon.SQS;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace A55.Subdivisions.Aws.Extensions;

public static class ServicesExtensions
{
    internal static IServiceCollection AddSubdivisionsHostedServices(this IServiceCollection services) =>
        services
            .AddSingleton<IConsumerFactory, ConsumerFactory>()
            .AddSingleton<IConsumerJob, ConcurrentConsumerJob>()
            .AddHostedService<SubdivisionsHostedService>();

    public static IServiceCollection AddSubdivisionsClient(
        this IServiceCollection services,
        Action<SubConfig>? config = null,
        AWSCredentials? credentials = null)
    {
        services
            .AddSingleton<IConfigureOptions<SubConfig>, ConfigureSubConfigOptions>()
            .PostConfigure<SubConfig>(c => config?.Invoke(c));

        services
            .AddSingleton(credentials ?? FallbackCredentialsFactory.GetCredentials())
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
            .AddSingleton<ISubMessageSerializer, SubJsonSerializer>()
            .AddTransient<ISubResourceManager, AwsResourceManager>()
            .AddTransient<ISubdivisionsClient, AwsSubClient>()
            .AddTransient<IProducer>(sp => sp.GetRequiredService<ISubdivisionsClient>())
            .AddTransient<IConsumerClient>(sp => sp.GetRequiredService<ISubdivisionsClient>());

        return services;
    }
}
