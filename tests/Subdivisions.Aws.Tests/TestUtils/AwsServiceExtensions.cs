using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using Subdivisions.Clients;
using Subdivisions.Extensions;
using Subdivisions.Models;
using Subdivisions.Services;

namespace Subdivisions.Aws.Tests.TestUtils;

static class AwsServiceExtensions
{
    public static async Task<MessageEnvelope[]> GetMessages(this IAmazonSQS sqs, ISubMessageSerializer serializer,
        TopicId topic)
    {
        var messages = await sqs.GetRawMessages(topic);

        return messages
            .Select(x => x.Body.EncodeAsUTF8())
            .Select(x => JsonSerializer.Deserialize<AwsSqs.SqsEnvelope>(x)!)
            .Select(x => serializer.Deserialize<MessageEnvelope>(x.Message)!)
            .ToArray();
    }

    public static async Task<Message[]> GetRawMessages(this IAmazonSQS sqs, TopicId topic)
    {
        var url = (await sqs.GetQueueUrlAsync(topic.QueueName)).QueueUrl;
        var messages = await sqs.ReceiveMessageAsync(url);
        return messages?.Messages.ToArray() ?? Array.Empty<Message>();
    }

    public static async Task<GetQueueAttributesResponse> GetQueueInfo(this IAmazonSQS sqs, string queue)
    {
        var url = (await sqs.GetQueueUrlAsync(queue)).QueueUrl;
        var info = await sqs.GetQueueAttributesAsync(url, new() { QueueAttributeName.All });
        return info;
    }

    public static async Task<(int Total, int Processing)> GetMessageStats(this IAmazonSQS sqs, string queue)
    {
        var info = await sqs.GetQueueInfo(queue);
        return (info.ApproximateNumberOfMessages, info.ApproximateNumberOfMessagesNotVisible);
    }

    public static async Task<bool> HasMessagesOn(this IAmazonSQS sqs, string queue) =>
        await sqs.GetMessageStats(queue) is { Total: > 0 };
}
