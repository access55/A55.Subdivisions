using A55.Subdivisions.Aws.Adapters;
using A55.Subdivisions.Aws.Tests.Builders;
using Amazon.EventBridge;
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

        result.Should().NotBeNullOrWhiteSpace();
        rule.Name.Should().Be(ruleBuilder.Topic.FullNamePascalCase);
        rule.State.Should().Be(RuleState.ENABLED);

        JToken.Parse(rule.EventPattern).Should().BeEquivalentTo(JToken.Parse(ruleBuilder.EventPattern));
    }
}
