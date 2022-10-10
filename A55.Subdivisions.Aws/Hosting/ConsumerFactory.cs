using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace A55.Subdivisions.Aws.Hosting;

class ConsumerFactory
{
    readonly IServiceProvider provider;
    readonly ILogger<ConsumerFactory> logger;
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

    public async Task ConsumeScoped(IConsumerDescriber describer, IMessage message, CancellationToken ctx)
    {
        using var _ =
            logger.BeginScope(
                $"Consumer[{describer.TopicName},{Environment.CurrentManagedThreadId}] Message[{message.Id}]");

        logger.LogInformation("Consuming message published at {MessageDate}", message.Datetime);
        await using var scope = provider.CreateAsyncScope();
        var instance = scope.ServiceProvider.GetRequiredService(describer.ConsumerType);

        if (instance is not IWeakConsumer consumer)
            throw new InvalidOperationException($"Invalid consumer type: {describer.ConsumerType}");

        var payload = describer.MessageType == typeof(string)
            ? message.Body
            : serializer.Deserialize(describer.MessageType, message.Body)
              ?? throw new NullReferenceException("Message body is NULL");

        stopwatch.Restart();
        try
        {
            await consumer.Consume(payload, ctx);
            await message.Delete();
        }
        catch (Exception ex)
        {
            logger.LogError(message: "Consumer error", ex);
            var handler = describer.ErrorHandler?.Invoke(ex) ?? Task.CompletedTask;
            await Task.WhenAll(message.Release(), handler);
        }
        finally
        {
            stopwatch.Stop();
            logger.LogInformation("Finished in {Time}ms", stopwatch.ElapsedMilliseconds);
        }
    }
}
