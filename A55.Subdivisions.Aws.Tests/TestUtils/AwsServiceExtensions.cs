using Amazon.SQS;

namespace A55.Subdivisions.Aws.Tests.TestUtils;

static class AwsServiceExtensions
{
    public static async Task<int> GetNumberOfMessages(this IAmazonSQS sqs, string queue)
    {
        var url = (await sqs.GetQueueUrlAsync(queue)).QueueUrl;
        var info = await sqs.GetQueueAttributesAsync(url, new() {QueueAttributeName.ApproximateNumberOfMessages});
        return info.ApproximateNumberOfMessages;
    }

    public static async Task<bool> HasMessagesOn(this IAmazonSQS sqs, string queue) =>
        await sqs.GetNumberOfMessages(queue) > 0;
}
