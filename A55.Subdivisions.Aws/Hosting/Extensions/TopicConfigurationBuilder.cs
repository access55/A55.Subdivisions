using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace A55.Subdivisions.Aws.Hosting.Extensions;

public sealed class TopicConfigurationBuilder<TMessage> where TMessage : notnull
{
    readonly IServiceCollection services;
    readonly string topicName;

    TimeSpan? pollingTime;
    int? concurrency;
    Type? consumerType;
    Func<Exception, Task>? errorHandler;

    public TopicConfigurationBuilder(IServiceCollection services, string topicName)
    {
        this.services = services;
        this.topicName = topicName;
    }

    public TopicConfigurationBuilder<TMessage> WithConsumer<TConsumer>(
        TimeSpan? pollingInterval = null,
        int? maxConcurrency = null)
        where TConsumer : class, IConsumer<TConsumer>
    {
        services.TryAddScoped<TConsumer>();
        this.consumerType = typeof(TConsumer);
        this.concurrency = maxConcurrency;
        this.pollingTime = pollingInterval;
        return this;
    }

    public TopicConfigurationBuilder<TMessage> OnError(Func<Exception, Task> handler)
    {
        this.errorHandler = handler;
        return this;
    }

    internal IConsumerDescriber CreateDescriber(IServiceProvider sp)
    {
        var settings = sp.GetRequiredService<IOptions<SubConfig>>().Value;
        var config =
            new ConsumerConfig
            {
                ErrorHandler = errorHandler,
                MaxConcurrency = concurrency ?? settings.QueueMaxReceiveCount,
                PollingInterval =
                    pollingTime ?? TimeSpan.FromSeconds(settings.PollingIntervalInSeconds),
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
}
