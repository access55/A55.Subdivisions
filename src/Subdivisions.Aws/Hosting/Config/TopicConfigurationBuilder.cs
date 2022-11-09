using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Subdivisions.Models;

namespace Subdivisions.Hosting.Config;

interface ITopicConfigurationBuilder
{
    bool HasConsumer { get; }
    IConsumerDescriber CreateConsumerDescriber(IServiceProvider sp);
}

public sealed class TopicConfigurationBuilder<TMessage> : ITopicConfigurationBuilder where TMessage : notnull
{
    readonly IServiceCollection services;
    readonly string topicName;
    int? concurrency;
    Type? consumerType;
    bool? useCompression;
    Func<Exception, Task>? errorHandler;

    TimeSpan? pollingTime;

    internal TopicConfigurationBuilder(IServiceCollection services, string topicName)
    {
        this.services = services;
        this.topicName = topicName;
        services.TryAddScoped<IProducer<TMessage>>(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<SubConfig>>().Value;
            return new TypedProducer<TMessage>(topicName, useCompression ?? settings.CompressMessages,
                sp.GetRequiredService<IProducerClient>());
        });
    }

    public bool HasConsumer => consumerType is not null;

    IConsumerDescriber ITopicConfigurationBuilder.CreateConsumerDescriber(IServiceProvider sp)
    {
        var settings = sp.GetRequiredService<IOptions<SubConfig>>().Value;
        var config =
            new ConsumerConfig
            {
                ErrorHandler = errorHandler,
                MaxConcurrency = concurrency ?? settings.QueueMaxReceiveCount,
                PollingInterval =
                    pollingTime ?? TimeSpan.FromSeconds(settings.PollingIntervalInSeconds)
            };

        if (config.PollingInterval < TimeSpan.FromSeconds(settings.LongPollingWaitInSeconds))
            throw new InvalidOperationException(
                $"{nameof(SubConfig.PollingIntervalInSeconds)} can't be less then {nameof(SubConfig.LongPollingWaitInSeconds)}");

        return new ConsumerDescriber(
            topicName,
            consumerType ?? throw new InvalidOperationException("Consumer type should be specified"),
            typeof(TMessage),
            config);
    }

    public TopicConfigurationBuilder<TMessage> Configure(
        TimeSpan? pollingInterval = null,
        int? maxConcurrency = null)
    {
        concurrency = maxConcurrency;
        pollingTime = pollingInterval;
        return this;
    }

    public TopicConfigurationBuilder<TMessage> WithConsumer<TConsumer>()
        where TConsumer : class, IConsumer<TMessage>
    {
        services.TryAddScoped<TConsumer>();
        consumerType = typeof(TConsumer);
        return this;
    }

    public TopicConfigurationBuilder<TMessage> WithConsumer(Delegate handler)
    {
        DelegateConsumer<TMessage>.ValidateParams(handler);
        services.TryAddScoped(sp => new DelegateConsumer<TMessage>(handler, sp));
        consumerType = typeof(DelegateConsumer<TMessage>);
        return this;
    }

    public TopicConfigurationBuilder<TMessage> DisableCompression()
    {
        useCompression = false;
        return this;
    }

    public TopicConfigurationBuilder<TMessage> EnableCompression()
    {
        useCompression = true;
        return this;
    }

    public TopicConfigurationBuilder<TMessage> OnError(Func<Exception, Task> handler)
    {
        errorHandler = handler;
        return this;
    }
}
