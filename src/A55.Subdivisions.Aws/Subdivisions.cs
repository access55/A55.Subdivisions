using A55.Subdivisions.Models;
using Amazon.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace A55.Subdivisions;

public static class Subdivisions
{
    public static ISubdivisionsClient CreateClient(
        Action<SubConfig>? config = null,
        AWSCredentials? credentials = null,
        Action<ILoggingBuilder>? logConfig = null
    ) =>
        new ServiceCollection()
            .AddSubdivisionsServices(config, credentials)
            .AddLogging(logConfig ?? delegate { })
            .BuildServiceProvider()
            .GetRequiredService<ISubdivisionsClient>();

    public static IProducerClient CreateProducer(
        Action<SubConfig>? config = null,
        AWSCredentials? credentials = null,
        Action<ILoggingBuilder>? logConfig = null
    ) => CreateClient(config, credentials, logConfig);

    public static IConsumerClient CreateConsumer(
        Action<SubConfig>? config = null,
        AWSCredentials? credentials = null,
        Action<ILoggingBuilder>? logConfig = null
    ) => CreateClient(config, credentials, logConfig);
}
