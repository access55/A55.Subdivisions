using System.Runtime.Serialization;
using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Subdivisions.Models;
using Subdivisions.Services;

namespace Subdivisions.Clients;

readonly record struct QueueInfo(Uri Url, SqsArn Arn);

interface IConsumeDriver
{
    Task<IReadOnlyCollection<IMessage>> ReceiveMessages(string queue, CancellationToken ctx);
    Task<IReadOnlyCollection<IMessage>> ReceiveDeadLetters(string queue, CancellationToken ctx);
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
    readonly IAmazonSQS sqs;

    public AwsSqs(
        ILogger<AwsSqs> logger,
        IOptions<SubConfig> config,
        ISubMessageSerializer serializer,
        IAmazonSQS sqs,
        AwsKms kms
    )
    {
        this.sqs = sqs;
        this.kms = kms;
        this.logger = logger;
        this.serializer = serializer;
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

    public async Task<bool> QueueExists(string queueName, CancellationToken ctx, bool deadLegger = false) =>
        await GetQueue(queueName, ctx, deadLegger) is not null;

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

        var q = await sqs.CreateQueueAsync(
            new CreateQueueRequest
            {
                QueueName = queueName,
                Attributes = new()
                {
                    [QueueAttributeName.RedrivePolicy] = JsonSerializer.Serialize(deadLetterPolicy),
                    [QueueAttributeName.Policy] = Iam,
                    [QueueAttributeName.KmsMasterKeyId] = keyId.Value,
                    [QueueAttributeName.VisibilityTimeout] = config.MessageTimeoutInSeconds.ToString(),
                    [QueueAttributeName.DelaySeconds] = config.MessageDelayInSeconds.ToString(),
                    [QueueAttributeName.MessageRetentionPeriod] = config.MessageRetantionInDays.ToString()
                }
            }, ctx);

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

    public async Task<IReadOnlyCollection<IMessage<string>>> ReceiveMessages(
        string queue,
        CancellationToken ctx)
    {
        if (await GetQueue(queue, ctx) is not { } queueInfo)
            throw new InvalidOperationException($"Unable to get '{queue}' data");

        var readMessagesRequest = await sqs.ReceiveMessageAsync(
            new ReceiveMessageRequest
            {
                QueueUrl = queueInfo.Url.ToString(),
                MaxNumberOfMessages = config.QueueMaxReceiveCount,
                WaitTimeSeconds = config.LongPollingWaitInSeconds,
                AttributeNames = new() {MessageSystemAttributeName.ApproximateReceiveCount}
            }, ctx);

        if (readMessagesRequest?.Messages is not { } messages)
            return ArraySegment<IMessage<string>>.Empty;

        return messages
            .Select(sqsMessage =>
            {
                var envelope = JsonSerializer.Deserialize<SqsEnvelope>(sqsMessage.Body) ??
                               throw new SerializationException("Unable to deserialize message");

                var message = serializer.Deserialize<MessageEnvelope>(envelope.Message);

                Task DeleteMessage()
                {
                    return sqs.DeleteMessageAsync(
                        new() {QueueUrl = queueInfo.Url.ToString(), ReceiptHandle = sqsMessage.ReceiptHandle},
                        CancellationToken.None);
                }

                Task ReleaseMessage(TimeSpan delay)
                {
                    return sqs.ChangeMessageVisibilityAsync(
                        new()
                        {
                            QueueUrl = queueInfo.Url.ToString(),
                            ReceiptHandle = sqsMessage.ReceiptHandle,
                            VisibilityTimeout = delay.Seconds
                        },
                        CancellationToken.None);
                }

                var receivedCount =
                    sqsMessage.Attributes
                        .TryGetValue(MessageSystemAttributeName.ApproximateReceiveCount, out var receiveString) &&
                    receiveString is not null &&
                    uint.TryParse(receiveString, out var received)
                        ? received
                        : 0;

                return new Message<string>(
                    message.MessageId,
                    message.Payload,
                    message.DateTime,
                    DeleteMessage,
                    ReleaseMessage,
                    receivedCount - 1);
            })
            .Cast<IMessage>()
            .ToArray();
    }

    public Task<IReadOnlyCollection<IMessage>> ReceiveDeadLetters(string queue, CancellationToken ctx) =>
        ReceiveMessages($"{DeadLetterPrefix}{queue}", ctx);

    class SqsEnvelope
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