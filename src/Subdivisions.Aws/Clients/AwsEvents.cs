using System.Reflection;
using System.Text.Json;
using Amazon.EventBridge;
using Amazon.EventBridge.Model;
using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Subdivisions.Extensions;
using Subdivisions.Models;
using Subdivisions.Services;

namespace Subdivisions.Clients;

interface IProduceDriver : IDisposable
{
    Task<PublishResult> Produce(TopicId topic, string message, Guid? correlationId,
        CancellationToken ctx);
}

sealed class AwsEvents : IProduceDriver
{
    readonly SubConfig config;
    readonly IAmazonEventBridge eventBridge;
    readonly ISubMessageSerializer serializer;
    readonly IDiagnostics diagnostics;
    readonly ILogger<AwsEvents> logger;
    readonly TagsService tags;
    readonly ISubClock clock;

    public AwsEvents(
        IAmazonEventBridge eventBridge,
        ISubMessageSerializer serializer,
        IDiagnostics diagnostics,
        ILogger<AwsEvents> logger,
        IOptions<SubConfig> config,
        TagsService tags,
        ISubClock clock)
    {
        this.eventBridge = eventBridge;
        this.serializer = serializer;
        this.diagnostics = diagnostics;
        this.logger = logger;
        this.tags = tags;
        this.clock = clock;
        this.config = config.Value;
    }

    public async Task<bool> RuleExists(TopicId topicId, CancellationToken ctx)
    {
        var rules = await eventBridge.ListRulesAsync(new()
        {
            Limit = 100,
            NamePrefix = topicId.TopicName
        }, ctx);

        return rules is not null &&
               rules.Rules.Any(r =>
                   r.Name.Trim() == topicId.TopicName && r.State == RuleState.ENABLED);
    }

    public async Task PutTarget(TopicId topic, SnsArn snsArn, CancellationToken ctx)
    {
        logger.LogInformation($"Putting EventBridge SNS target {topic.TopicName}[{snsArn.Value}]");

        var ruleTargets = await eventBridge.ListTargetsByRuleAsync(new()
        {
            Rule = topic.TopicName,
        }, ctx);

        if (ruleTargets.Targets.Any(x => x.Arn == snsArn.Value))
        {
            logger.LogInformation($"Target with {topic.TopicName}[{snsArn.Value}] already added");
            return;
        }

        var result = await eventBridge
            .PutTargetsAsync(
                new()
                {
                    Rule = topic.TopicName,
                    Targets = new List<Target>
                    {
                        new()
                        {
                            Id = topic.TopicName,
                            Arn = snsArn.Value,
                            InputPath = "$.detail"
                        }
                    }
                }, ctx);

        logger.LogInformation(
            $"Completed({result.HttpStatusCode}): EventBridge SNS target {topic.TopicName}[{snsArn.Value}]");
    }

    public async Task<RuleArn> CreateRule(TopicId topicId, CancellationToken ctx)
    {
        var eventPattern =
            $@"{{ ""detail-type"": [""{topicId.Event}""], ""detail"": {{ ""event"": [""{topicId.Event}""] }} }}";

        PutRuleRequest request = new()
        {
            Name = topicId.TopicName,
            Tags = tags.GetTags(x => new Tag
            {
                Key = x.Key,
                Value = x.Value
            }),
            Description =
                $"Created in {Assembly.GetExecutingAssembly().GetName().Name} for {topicId.TopicName} events",
            State = RuleState.ENABLED,
            EventBusName = "default",
            EventPattern = eventPattern
        };

        logger.LogInformation($"Creating EventBridge rule: {topicId.TopicName}");
        var response = await eventBridge.PutRuleAsync(request, ctx).ConfigureAwait(false);
        logger.LogDebug("Event Create/Update Response is: {Response}",
            response.HttpStatusCode);

        return new(response.RuleArn);
    }

    public async Task<PublishResult> Produce(TopicId topic, string message, Guid? correlationId,
        CancellationToken ctx)
    {
        var messageId = NewId.NextGuid();

        MessageEnvelope envelope = new(
            topic.Event,
            DateTime: clock.Now(),
            Payload: JsonDocument.Parse(message),
            MessageId: messageId,
            CorrelationId: correlationId
        );

        var body = serializer.Serialize(envelope).EncodeAsUtf8();
        logger.LogDebug($"Produce {topic.TopicName}: {body}");

        using var activity = diagnostics.StartProducerActivity(topic.TopicName);
        diagnostics.SetActivityMessageAttributes(
            activity, eventBridge.Config.ServiceURL, messageId, correlationId, body);

        PutEventsRequest request = new()
        {
            Entries = new()
            {
                new()
                {
                    DetailType = topic.Event,
                    Source = config.Source,
                    Detail = body
                }
            }
        };
        var response = await eventBridge.PutEventsAsync(request, ctx);

        if (response.FailedEntryCount > 0)
            throw new SubdivisionsException(string.Join(",",
                response.Entries.Select(x => x.ErrorMessage)));
        logger.LogDebug($"{topic}: Message produced on {topic.Event} - {response.HttpStatusCode}");
        diagnostics.AddProducedMessagesCounter(1, topic.RawName);
        return new(response.FailedEntryCount is 0, messageId, correlationId);
    }

    public void Dispose() => eventBridge.Dispose();
}
