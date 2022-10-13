using A55.Subdivisions.Models;
using A55.Subdivisions.Services;
using Amazon.SQS;
using Microsoft.Extensions.DependencyInjection;

namespace A55.Subdivisions.Aws.Tests.TestUtils.Fixtures;

public class SubClientFixture : LocalstackFixture
{
    protected DateTime fakedDate;
    protected IAmazonSQS sqs = null!;
    private protected TopicName Topic = null!;

    protected string TopicName => Topic.Topic;
    protected string QueueName => Topic.FullQueueName;

    [SetUp]
    public async Task Setup()
    {
        Topic = faker.TopicName(config);
        sqs = GetService<IAmazonSQS>();

        var resources = GetService<ISubResourceManager>();
        await resources.EnsureQueueExists(TopicName, default);

        fakedDate = faker.Date.Soon();
        A.CallTo(() => fakeClock.Now()).Returns(fakedDate);
    }

    protected async Task<IConsumerClient> CreateConsumer(Action<SubConfig>? configure = null) =>
        await NewSubClient(configure, isConsumer: true, isProducer: false);

    protected async Task<IProducerClient> CreateProducer(Action<SubConfig>? configure = null) =>
        await NewSubClient(configure, isConsumer: false, isProducer: true);

    protected Task<ISubdivisionsClient> CreateSubClient(Action<SubConfig>? configure = null) =>
        NewSubClient(configure, isConsumer: true, isProducer: true);

    async Task<ISubdivisionsClient> NewSubClient(
        Action<SubConfig>? configure,
        bool isConsumer,
        bool isProducer
    )
    {
        var services = CreateSubdivisionsServices(c =>
            {
                ConfigureSubdivisions(c);
                c.Prefix = config.Prefix;
                c.Suffix = config.Suffix;
                c.PubKey = config.PubKey;
                c.Source = $"s{Math.Abs(Guid.NewGuid().GetHashCode())}";
                configure?.Invoke(c);
                c.MessageDelayInSeconds = 0;
            })
            .AddSingleton(fakeClock);
        var provider = services.BuildServiceProvider();
        var resources = provider.GetRequiredService<ISubResourceManager>();

        if (isProducer)
            await resources.EnsureTopicExists(TopicName, default);

        if (isConsumer)
            await resources.EnsureQueueExists(TopicName, default);

        return provider.GetRequiredService<ISubdivisionsClient>();
    }
}
