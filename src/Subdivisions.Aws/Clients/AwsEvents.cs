using System.Reflection;
using Amazon.EventBridge;
using Amazon.EventBridge.Model;
using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Subdivisions.Extensions;
using Subdivisions.Models;
using Subdivisions.Services;

namespace Subdivisions.Clients;

interface IProduceDriver
{
    Task<PublishResult> Produce(TopicId topic, string message, Guid? correlationId, CancellationToken ctx);
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

    public async Task<bool> RuleExists(TopicId topicId, CancellationToken ctx)
    {
        var rules = await eventBridge.ListRulesAsync(new() { Limit = 100, NamePrefix = topicId.TopicName }, ctx);

        return rules is not null &&
               rules.Rules.Any(r => r.Name.Trim() == topicId.TopicName && r.State == RuleState.ENABLED);
    }

    public Task PutTarget(TopicId topic, SnsArn snsArn, CancellationToken ctx) => eventBridge
        .PutTargetsAsync(
            new()
            {
                Rule = topic.TopicName,
                Targets = new List<Target>
                {
                    new() {Id = topic.TopicName, Arn = snsArn.Value, InputPath = "$.detail"}
                }
            }, ctx);

    public async Task<RuleArn> CreateRule(TopicId topicId, CancellationToken ctx)
    {
        var eventPattern =
            $@"{{ ""detail-type"": [""{topicId.Event}""], ""detail"": {{ ""event"": [""{topicId.Event}""] }} }}";

        PutRuleRequest request = new()
        {
            Name = topicId.TopicName,
            Description =
                $"Created in {Assembly.GetExecutingAssembly().GetName().Name} for {topicId.TopicName} events",
            State = RuleState.ENABLED,
            EventBusName = "default",
            EventPattern = eventPattern
        };

        var response = await eventBridge.PutRuleAsync(request, ctx);
        logger.LogDebug("Event Create/Update Response is: {Response}", response.HttpStatusCode);

        return new(response.RuleArn);
    }

    public async Task<PublishResult> Produce(TopicId topic, string message, Guid? correlationId, CancellationToken ctx)
    {
        var messageId = NewId.NextGuid();
        MessageEnvelope messagePayload = new(topic.Event,
            DateTime: clock.Now(),
            Payload: message,
            MessageId: messageId,
            CorrelationId: correlationId
        );
        var payload = serializer.Serialize(messagePayload).EncodeAsUTF8();

        PutEventsRequest request = new()
        {
            Entries = new() { new() { DetailType = topic.Event, Source = config.Source, Detail = payload } }
        };
        var response = await eventBridge.PutEventsAsync(request, ctx);

        if (response.FailedEntryCount > 0)
            throw new SubdivisionsException(string.Join(",", response.Entries.Select(x => x.ErrorMessage)));

        return new(response.FailedEntryCount is 0, messageId, correlationId);
    }
}
