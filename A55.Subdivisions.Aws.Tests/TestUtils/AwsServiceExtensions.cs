using Amazon.SQS;
using Amazon.SQS.Model;

namespace A55.Subdivisions.Aws.Tests.TestUtils;

static class AwsServiceExtensions
{
    public static async Task<GetQueueAttributesResponse> GetQueueInfo(this IAmazonSQS sqs, string queue)
    {
        var url = (await sqs.GetQueueUrlAsync(queue)).QueueUrl;
        var info = await sqs.GetQueueAttributesAsync(url, new() {QueueAttributeName.All});
        return info;
    }

    public static async Task<(int Total, int Processing)> GetMessageStats(this IAmazonSQS sqs, string queue)
    {
        var info = await sqs.GetQueueInfo(queue);
        return (info.ApproximateNumberOfMessages, info.ApproximateNumberOfMessagesNotVisible);
    }

    public static async Task<bool> HasMessagesOn(this IAmazonSQS sqs, string queue) =>
        await sqs.GetMessageStats(queue) is {Total: > 0};
}
