using System.Reflection;
using Amazon;
using Amazon.EventBridge;
using Amazon.EventBridge.Model;
using Microsoft.Extensions.Logging;

namespace A55.Subdivisions.Aws.Adapters;

record RuleArn(string Value) : BaseArn(Value);

class AwsEvents
{
    readonly IAmazonEventBridge eventBridge;
    readonly ILogger<AwsEvents> logger;

    public AwsEvents(IAmazonEventBridge eventBridge, ILogger<AwsEvents> logger)
    {
        this.eventBridge = eventBridge;
        this.logger = logger;
    }

    public async Task<bool> RuleExists(TopicName topicName, CancellationToken ctx)
    {
        var rules = await eventBridge.ListRulesAsync(new() {Limit = 100, NamePrefix = topicName.FullTopicName}, ctx);

        return rules is not null &&
               rules.Rules.Any(r => r.Name.Trim() == topicName.FullTopicName && r.State == RuleState.ENABLED);
    }

    public Task PutTarget(string ruleName, SnsArn snsArn, CancellationToken ctx) => eventBridge
        .PutTargetsAsync(
            new()
            {
                Rule = ruleName,
                Targets = new List<Target> {new() {Id = ruleName, Arn = snsArn.Value, InputPath = "$.detail"}}
            }, ctx);

    public async Task<RuleArn> CreateRule(TopicName topicName, CancellationToken ctx)
    {
        var eventPattern =
            $@"{{ ""detail-type"": [""{topicName.Topic}""], ""detail"": {{ ""event"": [""{topicName.Topic}""] }} }}";

        PutRuleRequest request = new()
        {
            Name = topicName.FullTopicName,
            Description =
                $"Created in {Assembly.GetExecutingAssembly().GetName().Name} for {topicName.FullTopicName} events",
            State = RuleState.ENABLED,
            EventBusName = "default",
            EventPattern = eventPattern
        };

        var response = await eventBridge.PutRuleAsync(request, ctx);
        logger.LogDebug("Event Create/Update Response is: {Response}", response.HttpStatusCode);

        return new(response.RuleArn);
    }
}
