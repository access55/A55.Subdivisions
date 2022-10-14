using System.Diagnostics;
using A55.Subdivisions.Aws.Tests.TestUtils.Fixtures;
using A55.Subdivisions.Models;
using A55.Subdivisions.Services;
using Microsoft.Extensions.DependencyInjection;

namespace A55.Subdivisions.Aws.Tests.Specs.Integration;

public class SubClientMessageActionTests : SubClientFixture
{
    protected override void ConfigureSubdivisions(SubConfig c)
    {
        base.ConfigureSubdivisions(c);
        c.RetriesBeforeDeadLetter = 2;
    }

    [Test]
    public async Task ShouldDeleteMessageAfterProcessingIt()
    {
        var message = faker.Lorem.Lines();

        var client = (AwsSubClient)GetService<ISubdivisionsClient>();
        await client.Publish(Topic, message, default);

        var messages = await client.Receive(Topic, default);

        await messages.Single().Delete();

        (await sqs.GetMessageStats(Topic.FullQueueName))
            .Should().BeEquivalentTo((Processing: 0, Total: 0));
    }

    [Test]
    public async Task ShouldReleaseMessageBack()
    {
        var message = faker.Lorem.Lines();

        var client = (AwsSubClient)GetService<ISubdivisionsClient>();
        await client.Publish(Topic, message, default);

        var messages = await client.Receive(Topic, default);
        await messages.Single().Release(TimeSpan.Zero);

        (await sqs.HasMessagesOn(Topic.FullQueueName)).Should().BeTrue();
    }

    [Test]
    public async Task ShouldReleaseDelayMessage()
    {
        var message = faker.Lorem.Lines();
        var watch = new Stopwatch();

        var delay = TimeSpan.FromSeconds(5);
        var client = (AwsSubClient)GetService<ISubdivisionsClient>();

        await client.Publish(Topic, message, default);
        var messages = await client.Receive(Topic, default);
        await messages.Single().Release(delay);

        watch.Start();
        await Task.Delay(TimeSpan.FromSeconds(2));
        await WaitFor(async () => await sqs.HasMessagesOn(QueueName), next: TimeSpan.FromMilliseconds(100));
        watch.Stop();

        watch.Elapsed.Should().BeCloseTo(delay, TimeSpan.FromSeconds(1));
    }
}
