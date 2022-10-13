using Amazon.SQS;

namespace A55.Subdivisions.Aws.Tests.TestUtils;

static class AwsServiceExtensions
{
    public static async Task<(int Total, int Processing)> GetMetadata(this IAmazonSQS sqs, string queue)
    {
        var url = (await sqs.GetQueueUrlAsync(queue)).QueueUrl;
        var info = await sqs.GetQueueAttributesAsync(url, new() {QueueAttributeName.All});
        return (info.ApproximateNumberOfMessages, info.ApproximateNumberOfMessagesNotVisible);
    }

    public static async Task<bool> HasMessagesOn(this IAmazonSQS sqs, string queue) =>
        await sqs.GetMetadata(queue) is {Total: > 0};
}
