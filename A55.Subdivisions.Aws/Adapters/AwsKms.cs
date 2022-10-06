using System.ComponentModel.DataAnnotations;
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
    string? keyCache;

    public AwsKms(IAmazonKeyManagementService kms, IOptions<SubConfig> config)
    {
        this.kms = kms;
        this.config = config.Value;
    }

    public async ValueTask<string?> GetKey(CancellationToken ctx )
    {
        if (!string.IsNullOrWhiteSpace(keyCache))
            return keyCache;

        var aliases = await kms.ListAliasesAsync(new ListAliasesRequest {Limit = 100}, ctx);
        var key = aliases.Aliases.Find(x => x.AliasName == config.PubKey)?.TargetKeyId;
        if (string.IsNullOrWhiteSpace(keyCache))
            keyCache = key;
        return key;
    }
}
