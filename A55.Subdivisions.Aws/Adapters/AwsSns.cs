using System.Text.Json;
using Amazon;
using Amazon.SimpleNotificationService;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Logging;

namespace A55.Subdivisions.Aws.Adapters;

class AwsSns
{
    readonly IAmazonSimpleNotificationService sns;
    readonly ILogger<AwsSns> logger;
    public RegionEndpoint Region => sns.Config.RegionEndpoint;

    public AwsSns(IAmazonSimpleNotificationService sns, ILogger<AwsSns> logger)
    {
        this.sns = sns;
        this.logger = logger;
    }

}