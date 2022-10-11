using A55.Subdivisions.Aws.Models;
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
        await GetService<ISubResourceManager>().EnsureTopicExists(TopicName, default);
        fakedDate = faker.Date.Soon();
        A.CallTo(() => fakeClock.Now()).Returns(fakedDate);
    }

    public async Task<ISubdivisionsClient> NewSubClient(string source) =>
        await NewSubClient(c => c.Source = source);

    public async Task<ISubdivisionsClient> NewSubClient(Action<SubConfig> configure)
    {
        var services = CreateSubdivisionsServices(c =>
        {
            ConfigureSubdivisions(c);
            c.Prefix = config.Prefix;
            c.Suffix = config.Suffix;
            c.PubKey = config.PubKey;
            configure(c);
        });
        var provider = services.BuildServiceProvider();
        await provider.GetRequiredService<ISubResourceManager>().EnsureTopicExists(TopicName, default);

        return provider.GetRequiredService<ISubdivisionsClient>();
    }
}
