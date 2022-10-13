using A55.Subdivisions.Aws.Tests.TestUtils.Fixtures;
using A55.Subdivisions.Services;

namespace A55.Subdivisions.Aws.Tests.Specs.Integration;

public class SubClientMessageActionTests : SubClientFixture
{
    [Test]
    public async Task ShouldDeleteMessageAfterProcessingIt()
    {
        var message = faker.Lorem.Lines();

        var client = (AwsSubClient)GetService<ISubdivisionsClient>();
        await client.Publish(Topic, message, default);

        var messages = await client.Receive(Topic, default);

        await messages.Single().Delete();

        (await sqs.GetMetadata(Topic.FullQueueName))
            .Should().BeEquivalentTo((Processing: 0, Total: 0));
    }

    [Test]
    public async Task ShouldReleaseMessageBack()
    {
        var message = faker.Lorem.Lines();

        var client = (AwsSubClient)GetService<ISubdivisionsClient>();
        await client.Publish(Topic, message, default);

        var messages = await client.Receive(Topic, default);
        await messages.Single().Release();

        (await sqs.HasMessagesOn(Topic.FullQueueName)).Should().BeTrue();
    }
}
