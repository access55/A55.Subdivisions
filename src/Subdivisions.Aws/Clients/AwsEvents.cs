using System.Reflection;
using Amazon.EventBridge;
using Amazon.EventBridge.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Subdivisions.Models;

namespace Subdivisions.Clients;

sealed class AwsEvents
{
    readonly SubConfig config;
    readonly IAmazonEventBridge eventBridge;
    readonly ILogger<AwsEvents> logger;

    public AwsEvents(IAmazonEventBridge eventBridge, ILogger<AwsEvents> logger, IOptions<SubConfig> config)
    {
        this.eventBridge = eventBridge;
        this.logger = logger;
        this.config = config.Value;
    }

    public async Task<bool> RuleExists(TopicName topicName, CancellationToken ctx)
    {
        var rules = await eventBridge.ListRulesAsync(new() { Limit = 100, NamePrefix = topicName.FullTopicName }, ctx);

        return rules is not null &&
               rules.Rules.Any(r => r.Name.Trim() == topicName.FullTopicName && r.State == RuleState.ENABLED);
    }

    public Task PutTarget(TopicName topic, SnsArn snsArn, CancellationToken ctx) => eventBridge
        .PutTargetsAsync(
            new()
            {
                Rule = topic.FullTopicName,
                Targets = new List<Target>
                {
                    new() {Id = topic.FullTopicName, Arn = snsArn.Value, InputPath = "$.detail"}
                }
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

    public async Task<bool> PushEvent(TopicName topic, string message, CancellationToken ctx)
    {
        PutEventsRequest request = new()
        {
            Entries = new() { new() { DetailType = topic.Topic, Source = config.Source, Detail = message } }
        };
        var response = await eventBridge.PutEventsAsync(request, ctx);
        return response.FailedEntryCount is 0;
    }
}
