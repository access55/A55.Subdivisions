using A55.Subdivisions.Aws.Extensions;
using Amazon.EventBridge;
using Amazon.EventBridge.Model;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
using Docker.DotNet.Models;

namespace A55.Subdivisions.Aws.Tests.Specs.Integration;

public class AwsSubBootstraperTests : LocalstackFixture
{
    [Test]
    public async Task ShouldCreateAllTopicResources()
    {
        var topicName = Faker.TopicNameString();
        var bootstrapper = GetService<AwsSubdivisionsBootstrapper>();
        await CreateDefaultKmsKey();

        await bootstrapper.EnsureTopicExists(topicName);

        var resourses = await GetResourses();

        resourses.Queues.Should().Contain(x => x.Contains(topicName));
        resourses.Topic.TopicArn.Should().Contain(topicName.SnakeToPascalCase());
        resourses.Rule.Name.Should().Contain(topicName.SnakeToPascalCase());
    }

    public async Task<(Rule Rule, Topic Topic, string[] Queues)> GetResourses()
    {
        var ev = GetService<IAmazonEventBridge>();
        var sns = GetService<IAmazonSimpleNotificationService>();
        var sqs = GetService<IAmazonSQS>();

        var savedRules = ev.ListRulesAsync(new ListRulesRequest());
        var savedTopics = sns.ListTopicsAsync(new ListTopicsRequest());
        var savedQueues = sqs.ListQueuesAsync(new ListQueuesRequest());

        await Task.WhenAll(savedRules, savedTopics, savedQueues);

        return (
            savedRules.Result.Rules.Single(),
            savedTopics.Result.Topics.Single(),
            savedQueues.Result.QueueUrls.Select(Path.GetFileName).Cast<string>().ToArray()
        );
    }
}
