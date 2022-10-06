using System.Reflection;
using Amazon;
using Amazon.EventBridge;
using Amazon.EventBridge.Model;
using Microsoft.Extensions.Logging;

namespace A55.Subdivisions.Aws.Adapters;

record EventName(string Topic, string Prefix = "", string Sufix = "")
{
    public string Name { get; } =
        $"{Prefix.SnakeToPascalCase()}{Topic.SnakeToPascalCase()}{Sufix.SnakeToPascalCase()}".Trim();
}

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

    public async Task<bool> RuleExists(EventName eventName, CancellationToken ctx = default)
    {
        var rules = await eventBridge.ListRulesAsync(new()
        {
            Limit = 100,
            NamePrefix = eventName.Name,
        }, ctx);

        return rules is not null &&
               rules.Rules.Any(r => r.Name.Trim() == eventName.Name && r.State == RuleState.ENABLED);
    }

    public async Task<string> CreateRule(EventName eventName, CancellationToken ctx = default)
    {
        var eventPattern =
            $@"{{ ""detail-type"": [""{eventName.Topic}""], ""detail"": {{ ""event"": [""{eventName.Topic}""] }} }}";

        PutRuleRequest request = new()
        {
            Name = eventName.Name,
            Description = $"Created in {Assembly.GetExecutingAssembly().GetName().Name} for {eventName.Name} events",
            State = RuleState.ENABLED,
            EventBusName = "default",
            EventPattern = eventPattern,
        };

        var response = await eventBridge.PutRuleAsync(request, ctx);
        logger.LogDebug("Event Create/Update Response is: {Response}", response.HttpStatusCode);

        return response.RuleArn;
    }
}