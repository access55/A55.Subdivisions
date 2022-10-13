using A55.Subdivisions.Aws.Tests.Builders;
using A55.Subdivisions.Aws.Tests.TestUtils.Fixtures;
using A55.Subdivisions.Models;
using A55.Subdivisions.Services;

namespace A55.Subdivisions.Aws.Tests.Specs.Integration;

public class SubClientDeadLetterTests : SubClientFixture
{
    protected override void ConfigureSubdivisions(SubConfig c)
    {
        base.ConfigureSubdivisions(c);
        c.RetriesBeforeDeadLetter = 1;
    }

    [Test]
    public async Task MessageShouldGoToDeadLetterQueue()
    {
        var message = faker.Lorem.Lines();

        var client = GetService<ISubdivisionsClient>();
        await client.Publish(TopicName, message);

        var messages = await client.Receive(TopicName);
        await messages.Single().Release();

        var messageRetries = await client.Receive(TopicName);
        messageRetries.Should().BeEmpty();

        var deadMessages = await client.DeadLetters(TopicName);
        deadMessages.Should().BeEquivalentTo(new[] {new {Body = message, Datetime = fakedDate}});
    }

    [Test]
    public async Task ShouldDeserializeDeadLetter()
    {
        var message = TestMessage.New();

        var client = GetService<ISubdivisionsClient>();
        await client.Publish(TopicName, message);

        var messages = await client.Receive(TopicName);
        await messages.Single().Release();

        var messageRetries = await client.Receive(TopicName);
        messageRetries.Should().BeEmpty();

        var deadMessages = await client.DeadLetters<TestMessage>(TopicName);
        deadMessages.Should().BeEquivalentTo(new[] {new {Body = message, Datetime = fakedDate}});
    }
}

public class SubClientRetryTests : SubClientFixture
{
    protected override void ConfigureSubdivisions(SubConfig c)
    {
        base.ConfigureSubdivisions(c);
        c.RetriesBeforeDeadLetter = 2;
    }

    [Test]
    public async Task MessageShouldGoToDeadLetterQueue()
    {
        var message = faker.Lorem.Lines();
        var expectedMessage = new {Body = message, Datetime = fakedDate, RetryNumber = 0};

        var client = GetService<ISubdivisionsClient>();
        await client.Publish(TopicName, message);

        var messages1 = await client.Receive(TopicName);
        await messages1.Single().Release();
        messages1.Should().BeEquivalentTo(new[] {expectedMessage});

        var messages2 = await client.Receive(TopicName);
        await messages2.Single().Release();
        messages2.Should().BeEquivalentTo(new[] {expectedMessage with{ RetryNumber = 1 }});

        var messageRetries = await client.Receive(TopicName);
        messageRetries.Should().BeEmpty();

        var deadMessages = await client.DeadLetters(TopicName);
        messages2.Should().BeEquivalentTo(new[] {expectedMessage with{ RetryNumber = 1 }});
        deadMessages.Should().BeEquivalentTo(new[] {expectedMessage});
    }
}
