using System.Text.Json;
using A55.Subdivisions.Aws.Adapters;
using A55.Subdivisions.Aws.Models;
using A55.Subdivisions.Aws.Tests.Builders;
using A55.Subdivisions.Aws.Tests.TestUtils.Fixtures;
using Amazon.EventBridge;
using Amazon.EventBridge.Model;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
using FluentAssertions.Json;
using Newtonsoft.Json.Linq;

namespace A55.Subdivisions.Aws.Tests.Specs.Integration.Adapters;

public class AwsEventsTests : LocalstackFixture
{
    [Test]
    public async Task TopicExistsShouldReturnTrueIfRuleExists()
    {
        var eventClient = GetService<IAmazonEventBridge>();
        var rule = new EventRuleBuilder(config);

        await eventClient.PutRuleAsync(rule.CreateRule());

        var aws = GetService<AwsEvents>();
        var result = await aws.RuleExists(rule.Topic, default);

        result.Should().BeTrue();
    }

    [Test]
    public async Task TopicExistsShouldReturnFalseIfDisabled()
    {
        var eventClient = GetService<IAmazonEventBridge>();
        var rule = new EventRuleBuilder(config).Disabled();

        await eventClient.PutRuleAsync(rule.CreateRule());

        var aws = GetService<AwsEvents>();
        var result = await aws.RuleExists(rule.Topic, default);

        result.Should().BeFalse();
    }

    [Test]
    public async Task TopicExistsShouldReturnFalseIfNotExists()
    {
        var aws = GetService<AwsEvents>();
        var result = await aws.RuleExists(faker.TopicName(config), default);
        result.Should().BeFalse();
    }

    [Test]
    public async Task CreateRuleShouldPutNewRule()
    {
        var aws = GetService<AwsEvents>();
        var ruleBuilder = new EventRuleBuilder(config);
        var result = await aws.CreateRule(ruleBuilder.Topic, default);

        var eventClient = GetService<IAmazonEventBridge>();
        var rulesResponse = await eventClient.ListRulesAsync(new());
        var rule = rulesResponse.Rules.Single();

        result.Value.Should().NotBeNullOrWhiteSpace();
        rule.Name.Should().Be(ruleBuilder.Topic.FullTopicName);
        rule.State.Should().Be(RuleState.ENABLED);

        JToken.Parse(rule.EventPattern).Should().BeEquivalentTo(JToken.Parse(ruleBuilder.EventPattern));
    }

    [Test]
    public async Task ShouldPutTarget()
    {
        var ev = GetService<IAmazonEventBridge>();
        var sns = GetService<IAmazonSimpleNotificationService>();

        var rule = new EventRuleBuilder(config);
        var topicName = rule.Topic;
        await ev.PutRuleAsync(rule.CreateRule());
        var topic = await sns.CreateTopicAsync(new CreateTopicRequest {Name = topicName.FullTopicName});

        var aws = GetService<AwsEvents>();
        await aws.PutTarget(topicName, new SnsArn(topic.TopicArn), default);

        var target = await ev.ListTargetsByRuleAsync(new ListTargetsByRuleRequest {Rule = topicName.FullTopicName});

        target.Targets.Single().Arn.Should().Be(topic.TopicArn);
    }

    [Test]
    public async Task ShouldPushEvent()
    {
        var sut = GetService<AwsEvents>();
        var topic = faker.TopicName(config);
        var message = JsonSerializer.Serialize(new {@event = topic.Topic, Loren = faker.Lorem.Paragraph()});
        var queue = await SetupQueueRule(topic);

        var result = await sut.PushEvent(topic, message, default);
        result.IsSuccess.Should().BeTrue();

        var messages = await GetService<IAmazonSQS>().ReceiveMessageAsync(queue);
        messages.Messages.Single().Body.AsJToken().Should().BeEquivalentTo(message.AsJToken());
    }

    async Task<string> SetupQueueRule(TopicName topic)
    {
        var sqs = GetService<IAmazonSQS>();
        var ev = GetService<IAmazonEventBridge>();
        var createQueue = await sqs.CreateQueueAsync(new CreateQueueRequest {QueueName = topic.FullQueueName});
        var queue = await sqs.GetQueueAttributesAsync(createQueue.QueueUrl, new() {QueueAttributeName.QueueArn});

        await ev.PutRuleAsync(new PutRuleRequest
        {
            Name = topic.FullTopicName,
            State = RuleState.ENABLED,
            EventPattern =
                $@"{{ ""detail-type"": [""{topic.Topic}""], ""detail"": {{ ""event"": [""{topic.Topic}""] }} }}"
        });

        await ev.PutTargetsAsync(new PutTargetsRequest
        {
            Rule = topic.FullTopicName,
            Targets = new() {new() {Id = topic.FullTopicName, Arn = queue.QueueARN, InputPath = "$.detail"}}
        });

        return createQueue.QueueUrl;
    }
}
