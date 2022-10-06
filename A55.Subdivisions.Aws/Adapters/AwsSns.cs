using Amazon;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Amazon.SQS;
using Microsoft.Extensions.Logging;

namespace A55.Subdivisions.Aws.Adapters;

class AwsSns
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

    public async Task<string> CreateTopic(TopicName topicName, CancellationToken ctx)
    {
        var policy = GetPolicy(topicName, RegionEndpoint.USEast1);
        var keyId = await kms.GetKey(ctx) ??
                    throw new InvalidOperationException("Default KMS EncryptionKey Id not found");

        CreateTopicRequest request = new()
        {
            Name = topicName.FullName,
            Attributes = new() {[QueueAttributeName.KmsMasterKeyId] = keyId, [QueueAttributeName.Policy] = policy}
        };
        var response = await sns.CreateTopicAsync(request, ctx);
        logger.LogDebug("SNS Creation Response is: {Response}", response.HttpStatusCode);

        return response.TopicArn;
    }

    static string GetPolicy(string topicName, RegionEndpoint region) => @$"{{
""Version"": ""2008-10-17"",
""Id"": ""__default_policy_ID"",
""Statement"": [
    {{
        ""Sid"": ""__default_statement_ID"",
        ""Effect"": ""Allow"",
        ""Principal"": {{""AWS"": ""*""}},
        ""Action"": [
            ""SNS:GetTopicAttributes"",
            ""SNS:SetTopicAttributes"",
            ""SNS:AddPermission"",
            ""SNS:RemovePermission"",
            ""SNS:DeleteTopic"",
            ""SNS:Subscribe"",
            ""SNS:ListSubscriptionsByTopic"",
            ""SNS:Publish"",
            ""SNS:Receive"",
        ],
        ""Resource"": ""arn:aws:sns:{region.DisplayName}:*:{topicName}"",
    }},
    {{
        ""Sid"": ""Enable Eventbridge Events"",
        ""Effect"": ""Allow"",
        ""Principal"": {{""Service"": ""events.amazonaws.com""}},
        ""Action"": ""sns:Publish"",
        ""Resource"": ""*"",
    }},
],
}}";
}
