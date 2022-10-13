using System.Diagnostics;
using A55.Subdivisions.Models;
using A55.Subdivisions.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace A55.Subdivisions.Hosting;

interface IConsumerFactory
{
    Task ConsumeScoped<TMessage>(IConsumerDescriber describer, TMessage message, CancellationToken ctx)
        where TMessage : IMessage<string>;
}

class ConsumerFactory : IConsumerFactory
{
    readonly ILogger<ConsumerFactory> logger;
    readonly IServiceProvider provider;
    readonly ISubMessageSerializer serializer;
    readonly Stopwatch stopwatch = new();

    public ConsumerFactory(
        IServiceProvider provider,
        ILogger<ConsumerFactory> logger,
        ISubMessageSerializer serializer
    )

    {
        this.provider = provider;
        this.logger = logger;
        this.serializer = serializer;
    }

    public async Task ConsumeScoped<TMessage>(IConsumerDescriber describer, TMessage message, CancellationToken ctx)
        where TMessage : IMessage<string>
    {
        var header =
            $"-> {describer.TopicName}[{message.MessageId}]";

        logger.LogInformation("{Header} Consuming [published at {MessageDate}]", header, message.Datetime);
        await using var scope = provider.CreateAsyncScope();
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
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{Header} Consumer error", header);
            var handler = describer.ErrorHandler?.Invoke(ex) ?? Task.CompletedTask;
            await Task.WhenAll(message.Release(), handler);
        }
        finally
        {
            stopwatch.Stop();
            logger.LogInformation("{Header} Finished in {Time}ms", header, stopwatch.ElapsedMilliseconds);
        }
    }
}
