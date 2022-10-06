using A55.Subdivisions.Aws.Adapters;
using Microsoft.Extensions.Logging;

namespace A55.Subdivisions.Aws;

public class AwsSubBootstraper
{
    readonly AwsEvents events;
    readonly AwsSns sns;
    readonly AwsSqs sqs;

    internal AwsSubBootstraper(ILogger<AwsSubBootstraper> logger, AwsEvents events, AwsSns sns, AwsSqs sqs)
    {
        logger.LogInformation("Start Subdivisions. AWS Region is: {Region}", events.Region.DisplayName);
        this.events = events;
        this.sns = sns;
        this.sqs = sqs;
    }

    public async Task EnsureTopicExists()
    {
    }
}
