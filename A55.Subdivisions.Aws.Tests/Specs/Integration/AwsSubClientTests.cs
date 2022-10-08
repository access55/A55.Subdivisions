using A55.Subdivisions.Aws.Models;
using Amazon.SQS;

namespace A55.Subdivisions.Aws.Tests.Specs.Integration;

public class AwsSubClientFixtures : LocalstackFixture
{
    TopicName topic = null!;
    IAmazonSQS sqs = null!;

    [SetUp]
    public async Task Setup()
    {
        topic = Faker.TopicName();
        sqs = GetService<IAmazonSQS>();

        await CreateDefaultKmsKey();
        await GetService<AwsSubdivisionsBootstrapper>().EnsureTopicExists(topic, default);
    }

    [Test]
    public async Task ShouldSendAndReceiveMessages()
    {
        var message = Faker.Lorem.Lines();
        var fakedDate = Faker.Date.Soon();
        A.CallTo(() => FakeClock.Now()).Returns(fakedDate);

        var client = GetService<AwsSubClient>();
        await client.Publish(topic, message, default);

        await WaitFor(() => sqs.HasMessagesOn(topic.FullQueueName), TimeSpan.FromMinutes(1));

        var messages = await client.GetMessages(topic, default);

        messages.Should().BeEquivalentTo(new[] {new {Body = message, Datetime = fakedDate}});
    }
}
