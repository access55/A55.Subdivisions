using System.Reflection;
using Amazon.EventBridge;
using Amazon.EventBridge.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Subdivisions.Models;
using Subdivisions.Services;

namespace Subdivisions.Clients;

interface IProduceDriver
{
    Task<PublishResult> Produce(string topic, string message, CancellationToken ctx);
}

sealed class AwsEvents : IProduceDriver
{
    readonly SubConfig config;
    readonly IAmazonEventBridge eventBridge;
    readonly ISubMessageSerializer serializer;
    readonly ILogger<AwsEvents> logger;
    readonly ISubClock clock;

    public AwsEvents(
        IAmazonEventBridge eventBridge,
        ISubMessageSerializer serializer,
        ILogger<AwsEvents> logger,
        IOptions<SubConfig> config,
        ISubClock clock)
    {
        this.eventBridge = eventBridge;
        this.serializer = serializer;
        this.logger = logger;
        this.clock = clock;
        this.config = config.Value;
    }

    public async Task<bool> RuleExists(TopicName topicName, CancellationToken ctx)
    {
        var rules = await eventBridge.ListRulesAsync(new() {Limit = 100, NamePrefix = topicName.FullTopicName}, ctx);

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

    public async Task<PublishResult> Produce(string topic, string message, CancellationToken ctx)
    {
        var messageId = Guid.NewGuid();
        MessageEnvelope messagePayload = new(topic, clock.Now(), message, messageId);
        var payload = serializer.Serialize(messagePayload);

        PutEventsRequest request = new()
        {
            Entries = new() {new() {DetailType = topic, Source = config.Source, Detail = payload}}
        };
        var response = await eventBridge.PutEventsAsync(request, ctx);

        return new(response.FailedEntryCount is 0, messageId);
    }
}
