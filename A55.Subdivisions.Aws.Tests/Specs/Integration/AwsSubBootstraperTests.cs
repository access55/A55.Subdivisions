using A55.Subdivisions.Aws.Extensions;
using A55.Subdivisions.Aws.Tests.TestUtils.Fixtures;
using Amazon.EventBridge;
using Amazon.EventBridge.Model;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Amazon.SQS;
using Amazon.SQS.Model;

namespace A55.Subdivisions.Aws.Tests.Specs.Integration;

public class AwsSubBootstrapperTests : LocalstackFixture
{
    [Test]
    public async Task ShouldCreateAllTopicResources()
    {
        var topicName = faker.TopicNameString();
        var bootstrapper = GetService<ISubdivisionsBootstrapper>();

        await bootstrapper.EnsureTopicExists(topicName, default);

        var resourses = await GetResources();

        resourses.Queues.Should().Contain(x => x.Contains(topicName));
        resourses.Topic.TopicArn.Should().Contain(topicName.ToPascalCase());
        resourses.Rule.Name.Should().Contain(topicName.ToPascalCase());
    }

    public async Task<(Rule Rule, Topic Topic, string[] Queues)> GetResources()
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
