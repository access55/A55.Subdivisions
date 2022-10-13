using A55.Subdivisions.Models;
using Amazon.KeyManagementService;
using Amazon.KeyManagementService.Model;
using Microsoft.Extensions.Options;

namespace A55.Subdivisions.Clients;

sealed class AwsKms
{
    readonly SubConfig config;
    readonly IAmazonKeyManagementService kms;
    KeyId? keyCache;

    public AwsKms(IAmazonKeyManagementService kms, IOptions<SubConfig> config)
    {
        this.kms = kms;
        this.config = config.Value;
    }

    public async ValueTask<KeyId?> GetKey(CancellationToken ctx)
    {
        if (keyCache is not null)
            return keyCache;

        var aliases = await kms.ListAliasesAsync(new ListAliasesRequest { Limit = 100 }, ctx);
        var key = aliases.Aliases.Find(x => x.AliasName == config.PubKey)?.TargetKeyId;

        if (string.IsNullOrWhiteSpace(key))
            return null;

        keyCache = new(key);
        return keyCache;
    }

    public record struct KeyId(string Value);
}
