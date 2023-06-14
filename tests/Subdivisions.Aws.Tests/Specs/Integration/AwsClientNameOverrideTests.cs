using Amazon.SQS;
using Subdivisions.Aws.Tests.TestUtils;
using Subdivisions.Aws.Tests.TestUtils.Fixtures;
using Subdivisions.Models;
using Subdivisions.Services;
using TestMessage = Subdivisions.Aws.Tests.Builders.TestMessage;

namespace Subdivisions.Aws.Tests.Specs.Integration;

public class AwsClientNameOverrideTests : LocalstackFixture
{
    const string DefaultPrefix = "Prefix";
    const string DefaultSuffix = "Suffix";
    const string DefaultSource = "test";

    protected override void ConfigureSubdivisions(SubConfig c)
    {
        base.ConfigureSubdivisions(c);
        c.Prefix = DefaultPrefix;
        c.Suffix = DefaultSuffix;
        c.Source = DefaultSource;
    }

    [Test]
    public async Task ShouldUseCustomTopicPrefix()
    {
        const string customPrefix = "Custom";
        const string topicName = "myTopic";
        var client = GetService<ISubdivisionsClient>();
        var nameOverride = new TopicNameOverride
        {
            Prefix = customPrefix
        };
        var message = TestMessage.New().ToJson();

        await GetService<ISubResourceManager>()
            .EnsureQueueExists(topicName, nameOverride, default);

        await client.Publish(topicName, message, null,
            new()
            {
                NameOverride = nameOverride
            });

        var topic = new TopicId(topicName, new SubTopicNameConfig
        {
            Source = DefaultSource,
            Suffix = DefaultSuffix,
            Prefix = customPrefix,
        });

        var sqs = GetService<IAmazonSQS>();
        await WaitFor(() => sqs.HasMessagesOn(topic));
        var received = await sqs.GetMessages(GetService<ISubMessageSerializer>(), topic);

        var payload = received.Single().Payload.ToJsonString();
        payload.Should().Be(message);
    }

    [Test]
    public async Task ShouldUseCustomTopicSuffix()
    {
        const string customSuffix = "Custom";
        const string topicName = "myTopic";
        var client = GetService<ISubdivisionsClient>();
        var nameOverride = new TopicNameOverride
        {
            Suffix = customSuffix
        };
        var message = TestMessage.New().ToJson();

        await GetService<ISubResourceManager>()
            .EnsureQueueExists(topicName, nameOverride, default);

        await client.Publish(topicName, message, null,
            new()
            {
                NameOverride = nameOverride
            });

        var topic = new TopicId(topicName, new SubTopicNameConfig
        {
            Source = DefaultSource,
            Prefix = DefaultPrefix,
            Suffix = customSuffix,
        });

        var sqs = GetService<IAmazonSQS>();
        await WaitFor(() => sqs.HasMessagesOn(topic));
        var received = await sqs.GetMessages(GetService<ISubMessageSerializer>(), topic);

        var payload = received.Single().Payload.ToJsonString();
        payload.Should().Be(message);
    }
}
