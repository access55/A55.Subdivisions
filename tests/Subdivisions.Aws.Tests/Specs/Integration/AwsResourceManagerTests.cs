using Amazon.EventBridge;
using Amazon.EventBridge.Model;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
using Subdivisions.Aws.Tests.TestUtils;
using Subdivisions.Aws.Tests.TestUtils.Fixtures;
using Subdivisions.Extensions;
using Subdivisions.Services;

namespace Subdivisions.Aws.Tests.Specs.Integration;

public class AwsResourceManagerTests : LocalstackFixture
{
    [Test]
    public async Task ShouldCreateAllTopicResources()
    {
        var topicName = faker.TopicNameString();
        var bootstrapper = GetService<ISubResourceManager>();

        await bootstrapper.EnsureTopicExists(topicName, default);

        var resources = await GetResources();
        resources.Topic.Single().TopicArn.Should().Contain(topicName.ToPascalCase());
        resources.Rule.Single().Name.Should().Contain(topicName.ToPascalCase());
        resources.Queues.Should().BeEmpty();
    }

    [Test]
    public async Task ShouldCreateAllQueueResources()
    {
        var topicName = faker.TopicNameString();
        var bootstrapper = GetService<ISubResourceManager>();

        await bootstrapper.EnsureQueueExists(topicName, default);

        var resources = await GetResources();
        resources.Rule.Should().BeEmpty();
        resources.Topic.Single().TopicArn.Should().Contain(topicName.ToPascalCase());
        resources.Queues.Should().Contain(x => x.Contains(topicName));
    }

    public async Task<(Rule[] Rule, Topic[] Topic, string[] Queues)> GetResources()
    {
        var ev = GetService<IAmazonEventBridge>();
        var sns = GetService<IAmazonSimpleNotificationService>();
        var sqs = GetService<IAmazonSQS>();

        var savedRules = ev.ListRulesAsync(new ListRulesRequest());
        var savedTopics = sns.ListTopicsAsync(new ListTopicsRequest());
        var savedQueues = sqs.ListQueuesAsync(new ListQueuesRequest());

        await Task.WhenAll(savedRules, savedTopics, savedQueues);

        return (
            savedRules.Result.Rules.ToArray(),
            savedTopics.Result.Topics.ToArray(),
            savedQueues.Result.QueueUrls.Select(Path.GetFileName).Cast<string>().ToArray()
        );
    }
}
