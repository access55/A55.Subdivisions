using A55.Subdivisions.Aws.Tests.Builders;
using A55.Subdivisions.Aws.Tests.TestUtils.Fixtures;

namespace A55.Subdivisions.Aws.Tests.Specs.Integration;

public class SubClientTests : SubClientFixture
{
    [Test]
    public async Task ShouldSendAndReceiveMessages()
    {
        var message = faker.Lorem.Lines();

        var client = (AwsSubClient)GetService<ISubdivisionsClient>();
        await client.Publish(Topic, message, default);

        var messages = await client.Receive(Topic, default);

        messages.Should().BeEquivalentTo(new[] {new {Body = message, Datetime = fakedDate}});
    }

    [Test]
    public async Task ShouldSendAndReceiveMessagesOnClassPublicApi()
    {
        var stringTopicName = Topic.Topic;

        var message = faker.Lorem.Lines();

        var client = GetService<ISubdivisionsClient>();
        await client.Publish(stringTopicName, message);

        var messages = await client.Receive(stringTopicName);

        messages.Should().BeEquivalentTo(new[] {new {Body = message, Datetime = fakedDate}});
    }

    [Test]
    public async Task ShouldSendAndReceiveSerializedMessages()
    {
        var message = TestMessage.New();

        var stringTopicName = Topic.Topic;

        var client = GetService<ISubdivisionsClient>();
        await client.Publish(stringTopicName, message);

        var messages = await client.Receive<TestMessage>(stringTopicName);

        messages.Should().BeEquivalentTo(new[] {new {Body = message, Datetime = fakedDate}});
    }

    [Test]
    public async Task ShouldDeserializeSnakeCaseByDefault()
    {
        var strongMessage = TestMessage.New();
        var message = strongMessage.ToSnakeCaseJson();

        var stringTopicName = Topic.Topic;

        var client = GetService<ISubdivisionsClient>();
        await client.Publish(stringTopicName, message);

        var messages = await client.Receive<TestMessage>(stringTopicName);

        messages.Should().BeEquivalentTo(new[] {new {Body = strongMessage, Datetime = fakedDate}});
    }

    [Test]
    public async Task ShouldSerializeSnakeCaseByDefault()
    {
        var message = TestMessage.New();
        var jsonMessage = message.ToSnakeCaseJson().AsJToken();
        var stringTopicName = Topic.Topic;

        var client = GetService<ISubdivisionsClient>();
        await client.Publish(stringTopicName, message);

        var messages = await client.Receive(stringTopicName);

        messages.Single().Body.AsJToken()
            .Should()
            .BeEquivalentTo(jsonMessage);
    }
}
