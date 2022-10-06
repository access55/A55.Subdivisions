using System.Reflection;
using Amazon;
using Amazon.EventBridge;
using Amazon.EventBridge.Model;
using Microsoft.Extensions.Logging;

namespace A55.Subdivisions.Aws.Adapters;

class AwsEvents
{
    readonly IAmazonEventBridge eventBridge;
    readonly ILogger<AwsEvents> logger;
    public RegionEndpoint Region => eventBridge.Config.RegionEndpoint;

    public AwsEvents(IAmazonEventBridge eventBridge, ILogger<AwsEvents> logger)
    {
        this.eventBridge = eventBridge;
        this.logger = logger;
    }

    public async Task<bool> RuleExists(TopicName topicName, CancellationToken ctx )
    {
        var rules = await eventBridge.ListRulesAsync(new()
        {
            Limit = 100,
            NamePrefix = topicName.FullName,
        }, ctx);

        return rules is not null &&
               rules.Rules.Any(r => r.Name.Trim() == topicName.FullName && r.State == RuleState.ENABLED);
    }

    public async Task<string> CreateRule(TopicName topicName, CancellationToken ctx )
    {
        var eventPattern =
            $@"{{ ""detail-type"": [""{topicName.Topic}""], ""detail"": {{ ""event"": [""{topicName.Topic}""] }} }}";

        PutRuleRequest request = new()
        {
            Name = topicName.FullName,
            Description = $"Created in {Assembly.GetExecutingAssembly().GetName().Name} for {topicName.FullName} events",
            State = RuleState.ENABLED,
            EventBusName = "default",
            EventPattern = eventPattern,
        };

        var response = await eventBridge.PutRuleAsync(request, ctx);
        logger.LogDebug("Event Create/Update Response is: {Response}", response.HttpStatusCode);

        return response.RuleArn;
    }
}
