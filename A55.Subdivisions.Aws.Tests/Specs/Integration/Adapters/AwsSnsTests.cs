using A55.Subdivisions.Aws.Adapters;
using Amazon.KeyManagementService;
using Amazon.KeyManagementService.Model;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Amazon.SQS;
using Amazon.SQS.Model;

namespace A55.Subdivisions.Aws.Tests.Specs.Integration.Adapters;

public class AwsSnsTests : LocalstackTest
{
    [Test]
    public async Task ShouldCreateNewTopic()
    {
        var name = $"{Faker.Person.FirstName}_{Faker.Name.LastName()}_{Faker.Random.AlphaNumeric(6)}"
            .ToLowerInvariant();
        var topicName = new TopicName(name);
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
        var name = $"{Faker.Person.FirstName}_{Faker.Name.LastName()}_{Faker.Random.AlphaNumeric(6)}"
            .ToLowerInvariant();
        var topicName = new TopicName(name);
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
        var topicName = new TopicName(Faker.Random.String2(10).ToLowerInvariant());
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
        var topicName = new TopicName(Faker.Random.String2(10).ToLowerInvariant());

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
            TopicArn = topic.TopicArn, Protocol = "sqs", Endpoint = queue.QueueARN,
        });
    }
}
