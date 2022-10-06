using A55.Subdivisions.Aws.Adapters;
using Amazon.EventBridge;
using Amazon.KeyManagementService;
using Amazon.Runtime;
using Amazon.SimpleNotificationService;
using Amazon.SQS;
using Microsoft.Extensions.DependencyInjection;

namespace A55.Subdivisions.Aws;

public static class ServiceExtensions
{
    public static IServiceCollection AddSubdivisions(this IServiceCollection services, Action<SubConfig>? config = null,
        AWSCredentials? credentials = null, string? serviceUrl = null)
    {
        services.Configure<SubConfig>(c => config?.Invoke(c));

        AmazonSimpleNotificationServiceConfig configSqs = new();
        AmazonSQSConfig configSns = new();
        AmazonEventBridgeConfig configEventBridge = new();
        AmazonKeyManagementServiceConfig configKms = new();

        if (!string.IsNullOrWhiteSpace(serviceUrl))
        {
            configSns.ServiceURL =
                configKms.ServiceURL = configSqs.ServiceURL = configEventBridge.ServiceURL = serviceUrl;
        }

        services.AddSingleton(configSqs);
        services.AddSingleton(configSns);
        services.AddSingleton(configEventBridge);
        services.AddSingleton(configKms);

        services.AddSingleton(credentials ?? FallbackCredentialsFactory.GetCredentials());

        services.AddTransient<IAmazonSimpleNotificationService>(sp =>
        {
            var cred = sp.GetRequiredService<AWSCredentials>();
            var config = sp.GetRequiredService<AmazonSimpleNotificationServiceConfig>();
            return new AmazonSimpleNotificationServiceClient(cred, config);
        });

        services.AddTransient<IAmazonSQS>(sp =>
        {
            var cred = sp.GetRequiredService<AWSCredentials>();
            var config = sp.GetRequiredService<AmazonSQSConfig>();
            return new AmazonSQSClient(cred, config);
        });

        services.AddTransient<IAmazonEventBridge>(sp =>
        {
            var cred = sp.GetRequiredService<AWSCredentials>();
            var config = sp.GetRequiredService<AmazonEventBridgeConfig>();
            return new AmazonEventBridgeClient(cred, config);
        });

        services.AddTransient<IAmazonKeyManagementService>(sp =>
        {
            var cred = sp.GetRequiredService<AWSCredentials>();
            var config = sp.GetRequiredService<AmazonKeyManagementServiceConfig>();
            return new AmazonKeyManagementServiceClient(cred, config);
        });

        services.AddTransient<AwsEvents>();
        services.AddTransient<AwsSqs>();
        services.AddTransient<AwsSns>();
        services.AddTransient<AwsKms>();

        return services;
    }
}