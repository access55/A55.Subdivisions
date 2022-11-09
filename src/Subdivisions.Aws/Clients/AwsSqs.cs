using System.Runtime.Serialization;
using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Subdivisions.Extensions;
using Subdivisions.Models;
using Subdivisions.Services;

namespace Subdivisions.Clients;

readonly record struct QueueInfo(Uri Url, SqsArn Arn);

interface IConsumeDriver
{
    Task<IReadOnlyCollection<IMessage>> ReceiveMessages(TopicId topic, CancellationToken ctx);
    Task<IReadOnlyCollection<IMessage>> ReceiveDeadLetters(TopicId topic, CancellationToken ctx);
}

sealed class AwsSqs : IConsumeDriver
{
    const string DeadLetterPrefix = "dead_letter_";

    public static readonly string Iam = JsonSerializer.Serialize(new
    {
        Id = "SQSEventsPolicy",
        Version = "2012-10-17",
        Statement = new[]
        {
            new
            {
                Sid = "Allow_SQS_Services",
                Action = "sqs:*",
                Effect = "Allow",
                Resource = "arn:aws:sqs:*",
                Principal = new {AWS = "*"}
            }
        }
    });

    readonly SubConfig config;
    readonly AwsKms kms;
    readonly ILogger<AwsSqs> logger;
    readonly ISubMessageSerializer serializer;
    readonly ICompressor compressor;
    readonly IDiagnostics diagnostics;
    readonly IAmazonSQS sqs;

    public AwsSqs(
        ILogger<AwsSqs> logger,
        IOptions<SubConfig> config,
        ISubMessageSerializer serializer,
        ICompressor compressor,
        IDiagnostics diagnostics,
        IAmazonSQS sqs,
        AwsKms kms
    )
    {
        this.sqs = sqs;
        this.kms = kms;
        this.logger = logger;
        this.serializer = serializer;
        this.compressor = compressor;
        this.diagnostics = diagnostics;
        this.config = config.Value;
    }

    public async Task<QueueInfo> GetQueueAttributes(string queueUrl, CancellationToken ctx)
    {
        var response = await sqs.GetQueueAttributesAsync(queueUrl, new List<string> {QueueAttributeName.QueueArn}, ctx);
        logger.LogDebug("Queue Attributes Response is: {Response}", JsonSerializer.Serialize(response.Attributes));

        return new(new(queueUrl), new(response.QueueARN));
    }

    public async Task<QueueInfo?> GetQueue(string queueName,
        CancellationToken ctx, bool deadLetter = false)
    {
        var queue = $"{(deadLetter ? "dead_letter_" : string.Empty)}{queueName}";
        var responseQueues =
            await sqs.ListQueuesAsync(new ListQueuesRequest {QueueNamePrefix = queue, MaxResults = 1000}, ctx);

        var url = responseQueues.QueueUrls.Find(name => name.Contains(queue));
        if (url is null) return null;

        return await GetQueueAttributes(url, ctx);
    }

    public async Task<bool> QueueExists(string queueName, CancellationToken ctx, bool deadLetter = false) =>
        await GetQueue(queueName, ctx, deadLetter) is not null;

    public async Task<QueueInfo> CreateQueue(string queueName, CancellationToken ctx)
    {
        logger.LogDebug("Creating queue: {Name}", queueName);
        var keyId = await kms.GetKey(ctx) ??
                    throw new InvalidOperationException("Default KMS EncryptionKey Id not found");

        var deadLetter = await CreateDeadLetterQueue(queueName, keyId.Value, ctx);

        var deadLetterPolicy = new
        {
            deadLetterTargetArn = deadLetter.Arn.Value, maxReceiveCount = config.RetriesBeforeDeadLetter.ToString()
        };

        var createQueueRequest = new CreateQueueRequest
        {
            QueueName = queueName,
            Attributes = new()
            {
                [QueueAttributeName.RedrivePolicy] = JsonSerializer.Serialize(deadLetterPolicy),
                [QueueAttributeName.Policy] = Iam,
                [QueueAttributeName.KmsMasterKeyId] = keyId.Value,
                [QueueAttributeName.VisibilityTimeout] = config.MessageTimeoutInSeconds.ToString(),
                [QueueAttributeName.DelaySeconds] = config.MessageDelayInSeconds.ToString(),
                [QueueAttributeName.MessageRetentionPeriod] =
                    $"{(int)TimeSpan.FromDays(config.MessageRetentionInDays).TotalSeconds}"
            }
        };
        var q = await sqs.CreateQueueAsync(createQueueRequest, ctx);

        return await GetQueueAttributes(q.QueueUrl, ctx);
    }

    async Task<QueueInfo> CreateDeadLetterQueue(string queueName, string keyId, CancellationToken ctx)
    {
        var q = await sqs.CreateQueueAsync(
            new CreateQueueRequest
            {
                QueueName = $"{DeadLetterPrefix}{queueName}",
                Attributes = new() {["Policy"] = Iam, ["KmsMasterKeyId"] = keyId}
            }, ctx);
        return await GetQueueAttributes(q.QueueUrl, ctx);
    }

    async Task<IReadOnlyCollection<IMessage<string>>> ReceiveMessages(
        TopicId topic,
        bool deadletter,
        CancellationToken ctx)
    {
        var queue = deadletter ? $"{DeadLetterPrefix}{topic.QueueName}" : topic.QueueName;

        if (await GetQueue(queue, ctx) is not { } queueInfo)
            throw new InvalidOperationException($"Unable to get '{queue}' data");

        var queueUrl = queueInfo.Url.ToString();
        var readMessagesRequest = await sqs.ReceiveMessageAsync(
            new ReceiveMessageRequest
            {
                QueueUrl = queueUrl,
                MaxNumberOfMessages = config.QueueMaxReceiveCount,
                WaitTimeSeconds = config.LongPollingWaitInSeconds,
                AttributeNames = new() {MessageSystemAttributeName.ApproximateReceiveCount}
            }, ctx);

        if (readMessagesRequest?.Messages is not { } messages)
            return ArraySegment<IMessage<string>>.Empty;

        var parsedMessages = messages
            .Select(async sqsMessage =>
            {
                using var activity = diagnostics.StartConsumerActivity(topic.TopicName);

                var payload = JsonSerializer.Deserialize<SqsEnvelope>(sqsMessage.Body.EncodeAsUTF8()) ??
                              throw new SerializationException("Unable to deserialize message");

                var envelope = serializer.Deserialize<MessageEnvelope>(payload.Message);
                var rawMessage = envelope.Payload;

                var messageContent = envelope.Compressed == true
                    ? await compressor.Decompress(rawMessage)
                    : rawMessage;

                diagnostics.SetActivityMessageAttributes(
                    activity, queueUrl, envelope.MessageId, envelope.CorrelationId, rawMessage, messageContent);

                Task DeleteMessage() =>
                    sqs.DeleteMessageAsync(
                        new() {QueueUrl = queueInfo.Url.ToString(), ReceiptHandle = sqsMessage.ReceiptHandle},
                        CancellationToken.None);

                Task ReleaseMessage(TimeSpan delay) =>
                    sqs.ChangeMessageVisibilityAsync(
                        new()
                        {
                            QueueUrl = queueInfo.Url.ToString(),
                            ReceiptHandle = sqsMessage.ReceiptHandle,
                            VisibilityTimeout = delay.Seconds
                        },
                        CancellationToken.None);

                var receivedCount =
                    sqsMessage.Attributes
                        .TryGetValue(MessageSystemAttributeName.ApproximateReceiveCount, out var receiveString) &&
                    receiveString is not null &&
                    uint.TryParse(receiveString, out var received)
                        ? received
                        : 0;

                var parsedMessage = new Message<string>(
                    id: envelope.MessageId,
                    body: messageContent,
                    datetime: envelope.DateTime,
                    deleteMessage: DeleteMessage,
                    releaseMessage: ReleaseMessage,
                    correlationId: envelope.CorrelationId,
                    queueUrl: queueUrl,
                    retryNumber: receivedCount - 1);

                return parsedMessage;
            });

        var result = (await Task.WhenAll(parsedMessages))
            .Cast<IMessage>()
            .ToArray();

        diagnostics.AddRetrievedMessages(result.Length);

        return result;
    }

    public Task<IReadOnlyCollection<IMessage>> ReceiveDeadLetters(TopicId topic, CancellationToken ctx) =>
        ReceiveMessages(topic, true, ctx);

    public Task<IReadOnlyCollection<IMessage>> ReceiveMessages(TopicId topic, CancellationToken ctx) =>
        ReceiveMessages(topic, false, ctx);

    internal class SqsEnvelope
    {
        public SqsEnvelope(Guid messageId, string message)
        {
            MessageId = messageId;
            Message = message;
        }

        public Guid MessageId { get; set; }
        public string Message { get; set; }
    }
}
