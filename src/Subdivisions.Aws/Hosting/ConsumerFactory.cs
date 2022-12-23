using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Subdivisions.Models;
using Subdivisions.Services;

namespace Subdivisions.Hosting;

interface IConsumerFactory
{
    Task ConsumeScoped<TMessage>(IConsumerDescriber describer, TMessage message,
        CancellationToken ctx)
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
            message.Body
        );

        var header =
            $"{describer.TopicName}[{message.CorrelationId?.ToString() ?? "EMPTY-CORRELATION-ID"}.{message.MessageId}]";

        logger.LogInformation("-> {Header}: Consuming {Location} [published at {MessageDate}]",
            header,
            message.Location(),
            message.Datetime);
        await using var scope = provider.CreateAsyncScope();

        using var correlationResolver =
            scope.ServiceProvider.GetRequiredService<ISubCorrelationIdContext>();
        correlationResolver.Create(message.CorrelationId);

        var instance = scope.ServiceProvider.GetRequiredService(describer.ConsumerType);
        if (instance is not IWeakConsumer consumer)
            throw new InvalidOperationException(
                $"Invalid consumer type: {describer.ConsumerType} in {header}");

        var payload = describer.MessageType == typeof(string)
            ? message.Body
            : serializer.Deserialize(describer.MessageType, message.Body)
              ?? throw new NullReferenceException($"Message body is NULL in {header}");

        stopwatch.Restart();
        try
        {
            await consumer.Consume(payload, MessageMeta.FromMessage(message), ctx);
            await message.Delete();
            logger.LogInformation("[DELETED]{Header}: {Location}", header, message.Location());
            diagnostics.AddConsumedMessagesCounter(1, describer.TopicName, stopwatch.Elapsed);
        }
        catch (ConsumerDelayMessageException ex)
        {
            logger.LogInformation("[DELAYED]{Header}: {Location} for {Time}", header,
                message.Location(), ex.Time);
            await message.Release(ex.Time);
        }
        catch (ConsumerIgnoreMessageException ex)
        {
            logger.LogInformation("[IGNORED]{Header}: {Message}", header,
                ex.Message);
            await message.Delete();
            diagnostics.AddIgnoredMessagesCounter(1, describer.TopicName, stopwatch.Elapsed);
            logger.LogInformation("[DELETED]{Header}: {Location}", header, message.Location());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[ERROR]{Header}: Failure consuming message", header);
            diagnostics.RecordException(activity, ex, header);
            diagnostics.AddFailedMessagesCounter(1, describer.TopicName, stopwatch.Elapsed);
            var retryStrategy = scope.ServiceProvider.GetRequiredService<IRetryStrategy>();
            var delay = retryStrategy.Evaluate(message.RetryNumber);
            logger.LogInformation("[RELEASING]{Header}: message in {Span}", header, delay);
            await message.Release(delay);

            if (describer.ErrorHandler is not null)
            {
                logger.LogInformation("[ERROR]{Header}: Calling custom error handler", header);
                await describer.ErrorHandler(ex);
            }

            if (config.Value.RaiseExceptions)
                throw;
        }
        finally
        {
            stopwatch.Stop();
            logger.LogInformation("[FINISH]{Header}: completed in {Time}ms", header,
                stopwatch.ElapsedMilliseconds);
        }
    }
}

public class ConsumerIgnoreMessageException : Exception
{
    public ConsumerIgnoreMessageException(string message) : base(message)
    {
    }
}

public class ConsumerDelayMessageException : Exception
{
    public TimeSpan Time { get; }

    public ConsumerDelayMessageException(TimeSpan time) : base($"delaying message for {time}") =>
        Time = time;
}
