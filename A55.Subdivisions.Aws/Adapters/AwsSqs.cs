using System.Runtime.Serialization;
using System.Text.Json;
using A55.Subdivisions.Aws.Models;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Message = A55.Subdivisions.Aws.Models.Message;

namespace A55.Subdivisions.Aws.Adapters;

record QueueInfo(Uri Url, SqsArn Arn);

class AwsSqs
{
    public static readonly string IAM = JsonSerializer.Serialize(new
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
        CancellationToken ctx, bool deadLegger = false)
    {
        var queue = $"{(deadLegger ? "dead_letter_" : string.Empty)}{queueName}";
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

        var deadLetter = await CreateDeadletterQueue(queueName, keyId.Value, ctx);

        var deadLetterPolicy = new
        {
            deadLetterTargetArn = deadLetter.Arn.Value, maxReceiveCount = config.QueueMaxReceiveCount.ToString()
        };

        var q = await sqs.CreateQueueAsync(
            new CreateQueueRequest
            {
                QueueName = queueName,
                Attributes = new()
                {
                    [QueueAttributeName.RedrivePolicy] = JsonSerializer.Serialize(deadLetterPolicy),
                    [QueueAttributeName.Policy] = IAM,
                    [QueueAttributeName.KmsMasterKeyId] = keyId.Value,
                    [QueueAttributeName.VisibilityTimeout] = config.MessageTimeoutInSeconds.ToString(),
                    [QueueAttributeName.DelaySeconds] = config.MessageDelayInSeconds.ToString(),
                    [QueueAttributeName.MessageRetentionPeriod] = config.MessageRetantionInDays.ToString()
                }
            }, ctx);

        return await GetQueueAttributes(q.QueueUrl, ctx);
    }

    async Task<QueueInfo> CreateDeadletterQueue(string queueName, string keyId, CancellationToken ctx)
    {
        var q = await sqs.CreateQueueAsync(
            new CreateQueueRequest
            {
                QueueName = $"dead_letter_{queueName}",
                Attributes = new() {["Policy"] = IAM, ["KmsMasterKeyId"] = keyId}
            }, ctx);
        return await GetQueueAttributes(q.QueueUrl, ctx);
    }

    public async Task<IReadOnlyCollection<Message>> ReceiveMessages(
        string queue,
        CancellationToken ctx)
    {
        var queueInfo = await GetQueue(queue, ctx);
        if (queueInfo is null)
            throw new InvalidOperationException($"Unable to get '{queue}' data");

        var readMessagesRequest = await sqs.ReceiveMessageAsync(
            new ReceiveMessageRequest
            {
                QueueUrl = queueInfo.Url.ToString(), MaxNumberOfMessages = config.QueueMaxReceiveCount
            }, ctx);

        if (readMessagesRequest?.Messages is not { } messages)
            return ArraySegment<Message>.Empty;

        return messages
            .Select(m =>
            {
                var body = JsonSerializer.Deserialize<SqsMessageBody>(m.Body) ??
                           throw new SerializationException("Unable to deserialize message");

                var message = serializer.Desserialize<MessagePayload>(body.Message) ??
                              throw new SerializationException("Unable to deserialize message");

                Task DeleteMessage()
                {
                    return sqs.DeleteMessageAsync(
                        new() {QueueUrl = queueInfo.Url.ToString(), ReceiptHandle = m.ReceiptHandle},
                        CancellationToken.None);
                }

                return new Message(body.MessageId, message, DeleteMessage);
            })
            .ToArray();
    }

    class SqsMessageBody
    {
        public SqsMessageBody(Guid messageId, string message)
        {
            MessageId = messageId;
            Message = message;
        }

        public Guid MessageId { get; set; }
        public string Message { get; set; }
    }
}
