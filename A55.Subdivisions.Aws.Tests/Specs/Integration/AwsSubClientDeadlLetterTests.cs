﻿using A55.Subdivisions.Aws.Models;
using A55.Subdivisions.Aws.Tests.Builders;
using A55.Subdivisions.Aws.Tests.TestUtils.Fixtures;

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

        var client = GetService<ISubClient>();
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

        var client = GetService<ISubClient>();
        await client.Publish(TopicName, message);

        var messages = await client.Receive(TopicName);
        await messages.Single().Release();

        var messageRetries = await client.Receive(TopicName);
        messageRetries.Should().BeEmpty();

        var deadMessages = await client.DeadLetters<TestMessage>(TopicName);
        deadMessages.Should().BeEquivalentTo(new[] {new {Body = message, Datetime = fakedDate}});
    }
}