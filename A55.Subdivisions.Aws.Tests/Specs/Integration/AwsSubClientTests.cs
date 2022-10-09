using A55.Subdivisions.Aws.Models;
using A55.Subdivisions.Aws.Tests.Builders;
using Amazon.SQS;

namespace A55.Subdivisions.Aws.Tests.Specs.Integration;

public class AwsSubClientTests : AwsSubClientFixture
{
    [Test]
    public async Task ShouldSendAndReceiveMessages()
    {
        var message = faker.Lorem.Lines();
        var fakedDate = faker.Date.Soon();
        A.CallTo(() => fakeClock.Now()).Returns(fakedDate);

        var client = GetService<AwsSubClient>();
        await client.Publish(topic, message, default);

        await WaitFor(() => sqs.HasMessagesOn(topic.FullQueueName), TimeSpan.FromMinutes(1));

        var messages = await client.Receive(topic, default);

        messages.Should().BeEquivalentTo(new[] {new {Body = message, Datetime = fakedDate}});
    }
}

public class AwsSubClientInterfaceTests : AwsSubClientFixture
{
    [Test]
    public async Task ShouldSendAndReceiveMessagesOnClassPublicApi()
    {
        var stringTopicName = topic.Topic;
        var queueName = topic.FullQueueName;

        var message = faker.Lorem.Lines();
        var fakedDate = faker.Date.Soon();
        A.CallTo(() => fakeClock.Now()).Returns(fakedDate);

        var client = GetService<ISubClient>();
        await client.Publish(stringTopicName, message, default);

        await WaitFor(() => sqs.HasMessagesOn(queueName), TimeSpan.FromMinutes(1));

        var messages = await client.Receive(stringTopicName, default);

        messages.Should().BeEquivalentTo(new[] {new {Body = message, Datetime = fakedDate}});
    }
}

public class AwsSubClientSerializerTests : AwsSubClientFixture
{
    [Test]
    public async Task ShouldSendAndReceiveSerializedMessages()
    {
        var message = TestMessage.New();

        var stringTopicName = topic.Topic;
        var queueName = topic.FullQueueName;

        var fakedDate = faker.Date.Soon();
        A.CallTo(() => fakeClock.Now()).Returns(fakedDate);

        var client = GetService<ISubClient>();
        await client.Publish<TestMessage>(stringTopicName, message, default);

        await WaitFor(() => sqs.HasMessagesOn(queueName), TimeSpan.FromMinutes(1));

        var messages = await client.Receive<TestMessage>(stringTopicName, default);

        messages.Should().BeEquivalentTo(new[] {new {Body = message, Datetime = fakedDate}});
    }

    [Test]
    public async Task ShouldDeserializeSnakeCaseByDefault()
    {
        var strongMessage = TestMessage.New();
        var message = strongMessage.ToSnakeCaseJson();

        var stringTopicName = topic.Topic;
        var queueName = topic.FullQueueName;

        var fakedDate = faker.Date.Soon();
        A.CallTo(() => fakeClock.Now()).Returns(fakedDate);

        var client = GetService<ISubClient>();
        await client.Publish(stringTopicName, message, default);

        await WaitFor(() => sqs.HasMessagesOn(queueName), TimeSpan.FromMinutes(1));

        var messages = await client.Receive<TestMessage>(stringTopicName, default);

        messages.Should().BeEquivalentTo(new[] {new {Body = strongMessage, Datetime = fakedDate}});
    }

    [Test]
    public async Task ShouldSerializeSnakeCaseByDefault()
    {
        var message = TestMessage.New();
        var jsonMessage = message.ToSnakeCaseJson().AsJToken();

        var stringTopicName = topic.Topic;
        var queueName = topic.FullQueueName;

        var fakedDate = faker.Date.Soon();
        A.CallTo(() => fakeClock.Now()).Returns(fakedDate);

        var client = GetService<ISubClient>();
        await client.Publish<TestMessage>(stringTopicName, message, default);

        await WaitFor(() => sqs.HasMessagesOn(queueName), TimeSpan.FromMinutes(1));

        var messages = await client.Receive(stringTopicName, default);

        messages.Single().Body.AsJToken()
            .Should()
            .BeEquivalentTo(jsonMessage);
    }
}

[Parallelizable(ParallelScope.Self)]
public class AwsSubClientFixture : LocalstackFixture
{
    protected IAmazonSQS sqs = null!;
    private protected TopicName topic = null!;

    [SetUp]
    public async Task Setup()
    {
        topic = faker.TopicName(config);
        sqs = GetService<IAmazonSQS>();

        await CreateDefaultKmsKey();
        await GetService<AwsSubdivisionsBootstrapper>().EnsureTopicExists(topic, default);
    }
}
