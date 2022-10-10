using System.Text.Json;
using A55.Subdivisions.Aws.Models;
using Amazon;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Amazon.SQS;
using Microsoft.Extensions.Logging;

namespace A55.Subdivisions.Aws.Clients;

sealed class AwsSns
{
    readonly AwsKms kms;
    readonly ILogger<AwsSns> logger;
    readonly IAmazonSimpleNotificationService sns;

    public AwsSns(IAmazonSimpleNotificationService sns, AwsKms kms, ILogger<AwsSns> logger)
    {
        this.sns = sns;
        this.kms = kms;
        this.logger = logger;
    }

    public async Task<SnsArn> CreateTopic(TopicName topicName, CancellationToken ctx)
    {
        var policy = GetPolicy(topicName.Topic, RegionEndpoint.USEast1);
        var keyId = await kms.GetKey(ctx) ??
                    throw new InvalidOperationException("Default KMS EncryptionKey Id not found");

        CreateTopicRequest request = new()
        {
            Name = topicName.FullTopicName,
            Attributes = new()
            {
                [QueueAttributeName.KmsMasterKeyId] = keyId.Value, [QueueAttributeName.Policy] = policy
            }
        };
        var response = await sns.CreateTopicAsync(request, ctx);
        logger.LogDebug("SNS Creation Response is: {Response}", response.HttpStatusCode);

        return new(response.TopicArn);
    }

    public Task Subscribe(SnsArn snsArn, SqsArn sqsArn, CancellationToken ctx) => sns.SubscribeAsync(
        new SubscribeRequest {TopicArn = snsArn.Value, Protocol = "sqs", Endpoint = sqsArn.Value}, ctx);

    static string GetPolicy(string resourceName, RegionEndpoint region) => JsonSerializer.Serialize(
        new
        {
            Version = "2008-10-17",
            Id = "__default_policy_ID",
            Statement = new object[]
            {
                new
                {
                    Sid = "__default_statement_ID",
                    Effect = "Allow",
                    Principal = new {AWS = "*"},
                    Action = new[]
                    {
                        "SNS:GetTopicAttributes", "SNS:SetTopicAttributes", "SNS:AddPermission",
                        "SNS:RemovePermission", "SNS:DeleteTopic", "SNS:Subscribe",
                        "SNS:ListSubscriptionsByTopic", "SNS:Publish", "SNS:Receive"
                    },
                    Resource = $"arn:aws:sns:{region.SystemName}:*:{resourceName}"
                },
                new
                {
                    Sid = "Enable Eventbridge Events",
                    Effect = "Allow",
                    Principal = new {Service = "events.amazonaws.com"},
                    Action = "sns:Publish",
                    Resource = "*"
                }
            }
        });
}
