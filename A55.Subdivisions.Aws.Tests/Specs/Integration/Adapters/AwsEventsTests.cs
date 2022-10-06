using A55.Subdivisions.Aws.Adapters;
using A55.Subdivisions.Aws.Tests.Builders;
using Amazon.EventBridge;
using Amazon.EventBridge.Model;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using AutoBogus;
using FluentAssertions.Json;
using Newtonsoft.Json.Linq;

namespace A55.Subdivisions.Aws.Tests.Specs.Integration.Adapters;

public class AwsEventsTests : LocalstackTest
{
    [Test]
    public async Task TopicExistsShouldReturnTrueIfRuleExists()
    {
        var eventClient = GetService<IAmazonEventBridge>();
        var rule = new EventRuleBuilder();

        await eventClient.PutRuleAsync(rule.CreateRule());

        var aws = GetService<AwsEvents>();
        var result = await aws.RuleExists(rule.Topic, default);

        result.Should().BeTrue();
    }

    [Test]
    public async Task TopicExistsShouldReturnFalseIfDisabled()
    {
        var eventClient = GetService<IAmazonEventBridge>();
        var rule = new EventRuleBuilder().Disabled();

        await eventClient.PutRuleAsync(rule.CreateRule());

        var aws = GetService<AwsEvents>();
        var result = await aws.RuleExists(rule.Topic, default);

        result.Should().BeFalse();
    }

    [Test]
    public async Task TopicExistsShouldReturnFalseIfNotExists()
    {
        var aws = GetService<AwsEvents>();
        var result = await aws.RuleExists(AutoFaker.Generate<TopicName>(), default);
        result.Should().BeFalse();
    }

    [Test]
    public async Task CreateRuleShouldPutNewRule()
    {
        var aws = GetService<AwsEvents>();
        var ruleBuilder = new EventRuleBuilder();
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

        var rule = new EventRuleBuilder();
        TopicName topicName = rule.Topic;
        await ev.PutRuleAsync(rule.CreateRule());
        var topic = await sns.CreateTopicAsync(new CreateTopicRequest {Name = topicName.FullTopicName});

        var aws = GetService<AwsEvents>();
        await aws.PutTarget(topicName.FullTopicName, new SnsArn(topic.TopicArn), default);

        var target = await ev.ListTargetsByRuleAsync(new ListTargetsByRuleRequest {Rule = topicName.FullTopicName});

        target.Targets.Single().Arn.Should().Be(topic.TopicArn);
    }
}
