using A55.Subdivisions.Aws.Adapters;
using A55.Subdivisions.Aws.Models;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Amazon.SQS;
using Amazon.SQS.Model;

namespace A55.Subdivisions.Aws.Tests.Specs.Integration.Adapters;

public class AwsSnsTests : LocalstackFixture
{
    [Test]
    public async Task ShouldCreateNewTopic()
    {
        var topicName = faker.TopicName(config);
        var aws = GetService<AwsSns>();
        await CreateDefaultKmsKey();

        await aws.CreateTopic(topicName, default);

        var sns = GetService<IAmazonSimpleNotificationService>();
        var topics = await sns.ListTopicsAsync();

        topics.Topics.Should().ContainSingle();
    }

    [Test]
    public async Task CreateTopicShouldBeIdempotent()
    {
        var topicName = faker.TopicName(config);
        var aws = GetService<AwsSns>();
        await CreateDefaultKmsKey();

        var response1 = await aws.CreateTopic(topicName, default);
        var response2 = await aws.CreateTopic(topicName, default);

        var sns = GetService<IAmazonSimpleNotificationService>();
        var topics = await sns.ListTopicsAsync();

        topics.Topics.Should().ContainSingle();
        response1.Should().Be(response2);
    }

    [Test]
    public async Task ShouldCreateNewTopicWithArn()
    {
        var topicName = faker.TopicName(config);
        var aws = GetService<AwsSns>();
        await CreateDefaultKmsKey();

        var result = await aws.CreateTopic(topicName, default);

        var sns = GetService<IAmazonSimpleNotificationService>();
        var topic = await sns.GetTopicAttributesAsync(new GetTopicAttributesRequest {TopicArn = result.Value});

        topic.Attributes.Should().HaveCountGreaterThan(0);
    }

    [Test]
    public async Task ShouldSubscribeTopic()
    {
        var topicName = faker.TopicName(config);

        var sns = GetService<IAmazonSimpleNotificationService>();
        var sqs = GetService<IAmazonSQS>();
        var topic = await sns.CreateTopicAsync(new CreateTopicRequest {Name = topicName.FullTopicName});
        var queueResponse = await sqs.CreateQueueAsync(new CreateQueueRequest {QueueName = topicName.FullQueueName});
        var queue = await sqs.GetQueueAttributesAsync(queueResponse.QueueUrl,
            new List<string> {QueueAttributeName.QueueArn});

        var aws = GetService<AwsSns>();

        await aws.Subscribe(new SnsArn(topic.TopicArn), new SqsArn(queue.QueueARN), default);

        var subs = await sns.ListSubscriptionsAsync();
        subs.Subscriptions.Should().ContainEquivalentOf(new
        {
            topic.TopicArn, Protocol = "sqs", Endpoint = queue.QueueARN
        });
    }
}
