using A55.Subdivisions.Aws.Adapters;
using Amazon.KeyManagementService.Model;
using Microsoft.Extensions.Logging;
using Subdivisions;

namespace A55.Subdivisions.Aws;

public class AwsSubClient : ISub
{
    readonly AwsEvents events;

    internal AwsSubClient(AwsEvents events, ILogger<AwsSubClient> logger)
    {
        logger.LogInformation("Start Subdivisions. AWS Region is: {Region}", events.Region.DisplayName);
        this.events = events;
    }

    public Task<string> GetStringMessages(int quantity)
    {
        throw new InvalidOperationException();
    }
}
