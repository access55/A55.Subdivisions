using A55.Subdivisions.Aws.Models;
using Amazon.SQS;

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

        await GetService<ISubdivisionsBootstrapper>().EnsureTopicExists(TopicName, default);

        fakedDate = faker.Date.Soon();
        A.CallTo(() => fakeClock.Now()).Returns(fakedDate);
    }
}
