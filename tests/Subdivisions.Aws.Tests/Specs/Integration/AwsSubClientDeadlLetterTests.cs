using System.Text.Json;
using Subdivisions.Aws.Tests.Builders;
using Subdivisions.Aws.Tests.TestUtils;
using Subdivisions.Aws.Tests.TestUtils.Fixtures;
using Subdivisions.Models;

namespace Subdivisions.Aws.Tests.Specs.Integration;

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
        var message = TestMessage.New().ToJson();

        var client = GetService<ISubdivisionsClient>();
        await client.Publish(TopicName, message);

        await WaitFor(() => sqs.HasMessagesOn(Topic));
        var messages = await client.Receive(TopicName);
        await messages.Single().Release(TimeSpan.Zero);

        await Task.Delay(1000);
        var messageRetries = await client.Receive(TopicName);
        messageRetries.Should().BeEmpty();

        await Task.Delay(1000);
        var deadMessages = await client.DeadLetters(TopicName);
        deadMessages.ShouldMessagesBeEquivalentTo(new[]
        {
            new
            {
                Body = message.ToJsonDocument(),
                Datetime = fakedDate
            }
        });
    }

    [Test]
    public async Task ShouldDeserializeDeadLetter()
    {
        var message = TestMessage.New();

        var client = GetService<ISubdivisionsClient>();
        await client.Publish(TopicName, message);

        await WaitFor(() => sqs.HasMessagesOn(Topic));
        var messages = await client.Receive(TopicName);
        await messages.Single().Release(TimeSpan.Zero);

        await WaitFor(() => sqs.HasMessagesOn(Topic));
        var messageRetries = await client.Receive(TopicName);
        messageRetries.Should().BeEmpty();

        await Task.Delay(1000);
        var deadMessages = await client.DeadLetters<TestMessage>(TopicName);
        deadMessages.Should().BeEquivalentTo(new[]
        {
            new
            {
                Body = message,
                Datetime = fakedDate
            }
        });
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
        var message = TestMessage.New().ToJson();
        var expectedMessage = new
        {
            Body = JsonDocument.Parse(message),
            Datetime = fakedDate,
            RetryNumber = 0
        };

        var client = GetService<ISubdivisionsClient>();
        await client.Publish(TopicName, message);

        var messages1 = await client.Receive(TopicName);
        await messages1.Single().Release(TimeSpan.Zero);
        messages1.ShouldMessagesBeEquivalentTo(new[]
        {
            expectedMessage
        });

        var messages2 = await client.Receive(TopicName);
        await messages2.Single().Release(TimeSpan.Zero);
        messages2.ShouldMessagesBeEquivalentTo(new[]
        {
            expectedMessage with
            {
                RetryNumber = 1
            }
        });

        var messageRetries = await client.Receive(TopicName);
        messageRetries.Should().BeEmpty();

        var deadMessages = await client.DeadLetters(TopicName);
        messages2.ShouldMessagesBeEquivalentTo(new[]
        {
            expectedMessage with
            {
                RetryNumber = 1
            }
        });
        deadMessages.ShouldMessagesBeEquivalentTo(new[]
        {
            expectedMessage
        });
    }
}
