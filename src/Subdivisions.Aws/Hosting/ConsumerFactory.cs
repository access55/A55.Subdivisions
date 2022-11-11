using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Subdivisions.Models;
using Subdivisions.Services;

namespace Subdivisions.Hosting;

interface IConsumerFactory
{
    Task ConsumeScoped<TMessage>(IConsumerDescriber describer, TMessage message, CancellationToken ctx)
        where TMessage : IMessage<string>;
}

class ConsumerFactory : IConsumerFactory
{
    readonly ILogger<ConsumerFactory> logger;
    readonly IDiagnostics diagnostics;
    readonly IOptions<SubConfig> config;
    readonly IServiceProvider provider;
    readonly ISubMessageSerializer serializer;
    readonly Stopwatch stopwatch = new();

    public ConsumerFactory(
        IServiceProvider provider,
        ILogger<ConsumerFactory> logger,
        IDiagnostics diagnostics,
        IOptions<SubConfig> config,
        ISubMessageSerializer serializer
    )

    {
        this.provider = provider;
        this.logger = logger;
        this.diagnostics = diagnostics;
        this.config = config;
        this.serializer = serializer;
    }

    public async Task ConsumeScoped<TMessage>(IConsumerDescriber describer, TMessage message,
        CancellationToken ctx)
        where TMessage : IMessage<string>
    {
        using var activity = diagnostics.StartProcessActivity(describer.TopicName);
        diagnostics.SetActivityMessageAttributes(activity,
            message.QueueUrl,
            message.MessageId,
            message.CorrelationId,
            message.Body,
            message.Body);

        var header =
            $"-> {describer.TopicName}[{message.CorrelationId?.ToString() ?? "EMPTY-CORRELATION-ID"}.{message.MessageId}]";

        logger.LogInformation("{Header} Consuming [published at {MessageDate}]", header, message.Datetime);
        await using var scope = provider.CreateAsyncScope();

        using var correlationResolver = scope.ServiceProvider.GetRequiredService<ISubCorrelationIdContext>();
        correlationResolver.Create(message.CorrelationId);

        var instance = scope.ServiceProvider.GetRequiredService(describer.ConsumerType);
        if (instance is not IWeakConsumer consumer)
            throw new InvalidOperationException($"Invalid consumer type: {describer.ConsumerType} in {header}");

        var payload = describer.MessageType == typeof(string)
            ? message.Body
            : serializer.Deserialize(describer.MessageType, message.Body)
              ?? throw new NullReferenceException($"Message body is NULL in {header}");

        stopwatch.Restart();
        try
        {
            await consumer.Consume(payload, ctx);
            await message.Delete();
            diagnostics.AddConsumedMessagesCounter(1, stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{Header} Consumer error", header);

            diagnostics.RecordException(activity, ex, header);
            diagnostics.AddFailedMessagesCounter(1, stopwatch.Elapsed);

            var retryStrategy = scope.ServiceProvider.GetRequiredService<IRetryStrategy>();
            var delay = retryStrategy.Evaluate(message.RetryNumber);
            var handler = describer.ErrorHandler?.Invoke(ex) ?? Task.CompletedTask;
            await message.Release(delay);
            await handler;

            if (config.Value.RethrowExceptions)
                throw;
        }
        finally
        {
            stopwatch.Stop();
            logger.LogInformation("{Header} Finished in {Time}ms", header, stopwatch.ElapsedMilliseconds);
        }
    }
}
