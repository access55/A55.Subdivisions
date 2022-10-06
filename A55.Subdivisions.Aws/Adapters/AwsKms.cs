using System.Text.Json;
using Amazon;
using Amazon.KeyManagementService;
using Amazon.KeyManagementService.Model;
using Amazon.SimpleNotificationService;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Options;

namespace A55.Subdivisions.Aws.Adapters;

class AwsKms
{
    readonly IAmazonKeyManagementService kms;
    readonly SubConfig config;
    public RegionEndpoint Region => kms.Config.RegionEndpoint;

    public AwsKms(IAmazonKeyManagementService kms, IOptions<SubConfig> config)
    {
        this.kms = kms;
        this.config = config.Value;
    }

    public async Task<string?> GetKey()
    {
        var aliases = await kms.ListAliasesAsync(new ListAliasesRequest { Limit = 100 });
        return aliases.Aliases.FirstOrDefault(x => x.AliasName == config.PubKey)?.TargetKeyId;
    }
}