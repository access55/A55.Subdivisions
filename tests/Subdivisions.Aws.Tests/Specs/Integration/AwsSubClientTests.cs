using Subdivisions.Aws.Tests.Builders;
using Subdivisions.Aws.Tests.TestUtils;
using Subdivisions.Aws.Tests.TestUtils.Fixtures;
using Subdivisions.Services;

namespace Subdivisions.Aws.Tests.Specs.Integration;

public class SubClientTests : SubClientFixture
{
    [Test]
    public async Task ShouldSendAndReceiveMessages()
    {
        var message = faker.Lorem.Lines();
        var correlationId = Guid.NewGuid();

        var client = (AwsSubClient)GetService<ISubdivisionsClient>();
        var published = await client.Publish(Topic, message, correlationId, false, default);

        var messages = await client.Receive(Topic, default);

        messages.Should()
            .BeEquivalentTo(new[] { new { Body = message, Datetime = fakedDate, published.MessageId } });
    }

    [Test]
    public async Task ShouldSendAndReceiveCompressedMessages()
    {
        var message = faker.Lorem.Lines();
        var correlationId = Guid.NewGuid();

        var client = (AwsSubClient)GetService<ISubdivisionsClient>();
        var published = await client.Publish(Topic, message, correlationId, true, default);

        var messages = await client.Receive(Topic, default);

        messages.Should()
            .BeEquivalentTo(new[] { new { Body = message, Datetime = fakedDate, published.MessageId } });
    }

    [Test]
    public async Task ShouldCompressedMessageBeDifferent()
    {
        var message = faker.Lorem.Paragraphs();

        var client = (AwsSubClient)GetService<ISubdivisionsClient>();
        await client.Publish(Topic, message, null, true, default);

        var messages = await sqs.GetMessages(GetService<ISubMessageSerializer>(), Topic);

        var payload = messages.Single().Payload;
        payload.Length.Should().BeLessThan(message.Length);

        var compressor = GetService<ICompressor>();
        var body = await compressor.Decompress(payload);
        body.Should().Be(message);
    }

    [Test]
    public async Task ShouldSendAndReceiveMessagesOnClassPublicApi()
    {
        var stringTopicName = Topic.Event;

        var correlationId = Guid.NewGuid();
        var message = faker.Lorem.Lines();

        var client = GetService<ISubdivisionsClient>();
        var published = await client.Publish(stringTopicName, message, correlationId);

        var messages = await client.Receive(stringTopicName);

        messages.Should().BeEquivalentTo(new[]
        {
            new {Body = message, Datetime = fakedDate, published.MessageId, CorrelationId = correlationId}
        });
    }

    [Test]
    public async Task ShouldSendAndReceiveSerializedMessages()
    {
        var message = TestMessage.New();
        var correlationId = Guid.NewGuid();

        var stringTopicName = Topic.Event;

        var client = GetService<ISubdivisionsClient>();
        await client.Publish(stringTopicName, message, correlationId);

        var messages = await client.Receive<TestMessage>(stringTopicName);

        messages.Should()
            .BeEquivalentTo(new[] { new { Body = message, Datetime = fakedDate, CorrelationId = correlationId } });
    }

    [Test]
    public async Task ShouldDeliverMessagesToAllConsumers()
    {
        var message = TestMessage.New();

        var producer = await CreateProducer();
        var consumer1 = GetService<IConsumerClient>();
        var consumer2 = await CreateConsumer();
        var consumer3 = await CreateConsumer();

        var published = await producer.Publish(TopicName, message);

        await WaitFor(() => sqs.HasMessagesOn(Topic.QueueName));
        var messages1 = await consumer1.Receive<TestMessage>(TopicName);
        await Task.Delay(1000);
        var messages2 = await consumer2.Receive<TestMessage>(TopicName);
        var messages3 = await consumer3.Receive<TestMessage>(TopicName);

        var expected = new[] { new { Body = message, Datetime = fakedDate, published.MessageId } };
        messages1.Should().BeEquivalentTo(expected);
        messages2.Should().BeEquivalentTo(expected);
        messages3.Should().BeEquivalentTo(expected);
    }

    [Test]
    public async Task ShouldDeserializeSnakeCaseByDefault()
    {
        var strongMessage = TestMessage.New();
        var message = strongMessage.ToSnakeCaseJson();

        var stringTopicName = Topic.Event;

        var client = GetService<ISubdivisionsClient>();
        await client.Publish(stringTopicName, message);

        var messages = await client.Receive<TestMessage>(stringTopicName);

        messages.Should().BeEquivalentTo(new[] { new { Body = strongMessage, Datetime = fakedDate } });
    }

    [Test]
    public async Task ShouldSerializeSnakeCaseByDefault()
    {
        var message = TestMessage.New();
        var jsonMessage = message.ToSnakeCaseJson().AsJToken();
        var stringTopicName = Topic.Event;

        var client = GetService<ISubdivisionsClient>();
        await client.Publish(stringTopicName, message);

        var messages = await client.Receive(stringTopicName);

        messages.Single().Body.AsJToken()
            .Should()
            .BeEquivalentTo(jsonMessage);
    }
}
