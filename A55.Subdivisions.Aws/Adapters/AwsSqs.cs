using System.Text.Json;
using Amazon;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace A55.Subdivisions.Aws.Adapters;

record QueueInfo(string Url, string Arn);

class AwsSqs
{
    readonly IAmazonSQS sqs;
    readonly AwsKms kms;
    readonly ILogger<AwsSqs> logger;
    readonly SubConfig config;
    public RegionEndpoint Region => sqs.Config.RegionEndpoint;

    public AwsSqs(IAmazonSQS sqs, AwsKms kms, ILogger<AwsSqs> logger, IOptions<SubConfig> config)
    {
        this.sqs = sqs;
        this.kms = kms;
        this.logger = logger;
        this.config = config.Value;
    }

    public async Task<QueueInfo> GetQueueAttributes(string queueUrl, CancellationToken ctx = default)
    {
        var response = await sqs.GetQueueAttributesAsync(queueUrl, new List<string> {QueueAttributeName.QueueArn});
        logger.LogDebug("Queue Attributes Response is: {Response}", JsonSerializer.Serialize(response.Attributes));

        return new(queueUrl, response.QueueARN);
    }

    public async Task<QueueInfo?> GetQueue(string queueName, bool deadLegger = false,
        CancellationToken ctx = default)
    {
        var queue = $"{(deadLegger ? "dead_letter_" : string.Empty)}{queueName}";
        var responseQueues = await sqs.ListQueuesAsync(new ListQueuesRequest
        {
            QueueNamePrefix = queue,
            MaxResults = 1000,
        }, ctx);

        var url = responseQueues.QueueUrls.FirstOrDefault(name => name.Contains(queue));
        if (url is null) return null;

        return await GetQueueAttributes(url, ctx);
    }

    public async Task<bool> QueueExists(string queueName, bool deadLegger = false, CancellationToken ctx = default) =>
        await GetQueue(queueName, deadLegger, ctx) is not null;

    public async Task<QueueInfo> CreateQueue(string queueName)
    {
        logger.LogDebug("Creating queue: {Name}", queueName);
        var keyId = await kms.GetKey() ?? throw new InvalidOperationException("Default KMS EncryptionKey Id not found");

        var deadLetter = await CreateDeadletterQueue(queueName, keyId);

        var deadLetterPolicy = new
            {deadLetterTargetArn = deadLetter.Arn, maxReceiveCount = config.QueueMaxReceiveCount.ToString()};

        var q = await sqs.CreateQueueAsync(new CreateQueueRequest
        {
            QueueName = queueName,
            Attributes = new()
            {
                [QueueAttributeName.RedrivePolicy] = JsonSerializer.Serialize(deadLetterPolicy),
                [QueueAttributeName.Policy] = IAM,
                [QueueAttributeName.KmsMasterKeyId] = keyId,
            }
        });

        return await GetQueueAttributes(q.QueueUrl);
    }

    async Task<QueueInfo> CreateDeadletterQueue(string queueName, string keyId)
    {
        var q = await sqs.CreateQueueAsync(new CreateQueueRequest
        {
            QueueName = $"dead_letter_{queueName}",
            Attributes = new()
            {
                ["Policy"] = IAM,
                ["KmsMasterKeyId"] = keyId,
            }
        });
        return await GetQueueAttributes(q.QueueUrl);
    }

    const string IAM = @"
{
  ""Id"": ""SQSEventsPolicy"",
  ""Version"": ""2012-10-17"",
  ""Statement"": [
    {
      ""Sid"": ""Allow_SQS_Services"",
      ""Action"": ""sqs:*"",
      ""Effect"": ""Allow"",
      ""Resource"": ""arn:aws:sqs:*"",
      ""Principal"": {
        ""AWS"": ""*""
      }
    }
  ]
}";
}