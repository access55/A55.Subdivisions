using Amazon.EventBridge;
using Amazon.KeyManagementService;
using Amazon.Runtime;
using Amazon.SimpleNotificationService;
using Amazon.SQS;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Subdivisions.Clients;
using Subdivisions.Extensions;
using Subdivisions.Hosting;
using Subdivisions.Models;
using Subdivisions.Services;

namespace Subdivisions;

public static class ServicesExtensions
{
    public static IServiceCollection AddSubdivisionsServices(
        this IServiceCollection services,
        Action<SubConfig>? config = null,
        AWSCredentials? credentials = null)
    {
        services
            .AddSingleton<IConfigureOptions<SubAwsCredentialsConfig>,
                SubAwsCredentialsConfigOptions>()
            .AddSingleton<IConfigureOptions<SubConfig>, ConfigureSubConfigOptions>()
            .PostConfigure<SubConfig>(c =>
            {
                config?.Invoke(c);
                if (c is { Localstack: true, ServiceUrl: null })
                    c.ServiceUrl = "http://localhost:4566";

                if (string.IsNullOrWhiteSpace(c.Source))
                    throw new InvalidOperationException("Unable to infer the source name");
            });

        services
            .AddSingleton<SubAwsCredentialWrapper>(sp => new(sp.ResolveAwsCredential(credentials)));

        services
            .AddAwsConfig<AmazonSQSConfig>()
            .AddAwsConfig<AmazonEventBridgeConfig>()
            .AddAwsConfig<AmazonKeyManagementServiceConfig>()
            .AddAwsConfig<AmazonSimpleNotificationServiceConfig>();

        services
            .AddTransient<IAmazonSimpleNotificationService>(sp =>
                new AmazonSimpleNotificationServiceClient(
                    sp.GetAwsCredentials(),
                    sp.GetRequiredService<AmazonSimpleNotificationServiceConfig>()))
            .AddTransient<IAmazonSQS>(sp =>
                new AmazonSQSClient(sp.GetAwsCredentials(),
                    sp.GetRequiredService<AmazonSQSConfig>()))
            .AddTransient<IAmazonEventBridge>(sp =>
                new AmazonEventBridgeClient(sp.GetAwsCredentials(),
                    sp.GetRequiredService<AmazonEventBridgeConfig>()))
            .AddTransient<IAmazonKeyManagementService>(sp =>
                new AmazonKeyManagementServiceClient(sp.GetAwsCredentials(),
                    sp.GetRequiredService<AmazonKeyManagementServiceConfig>()));

        services
            .AddScoped<AwsKms>()
            .AddScoped<AwsEvents>()
            .AddScoped<AwsSqs>()
            .AddScoped<AwsSns>();

        services
            .AddSingleton<ISubClock, UtcClock>()
            .AddSingleton<TagsService>()
            .AddSingleton<IDiagnostics, Diagnostics>()
            .AddSingleton<IRetryStrategy, Power2RetryStrategy>()
            .AddSingleton<SubDefaultJsonSerializerConverters>()
            .AddSingleton<ISubJsonSerializerConverters, SubDefaultJsonSerializerConverters>()
            .AddSingleton<ISubJsonSerializerOptions, SubJsonSerializerOptions>()
            .AddSingleton<ISubMessageSerializer, SubJsonSerializer>()
            .AddScoped<ISubResourceManager, AwsResourceManager>()
            .AddScoped<IConsumeDriver, AwsSqs>()
            .AddScoped<IProduceDriver, AwsEvents>()
            .AddScoped<ICorrelationResolver, CorrelationResolver>()
            .AddScoped<ISubdivisionsClient, AwsSubClient>()
            .AddScoped<IProducerClient>(sp => sp.GetRequiredService<ISubdivisionsClient>())
            .AddScoped<IConsumerClient>(sp => sp.GetRequiredService<ISubdivisionsClient>());

        return services;
    }
}
