using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Subdivisions.Models;

public sealed class SubAwsCredentialsConfig
{
    [ConfigurationKeyName("SUBDIVISIONS_AWS_ACCESS_KEY_ID")]
    public string? SubdivisionsAwsAccessKey { get; set; }

    [ConfigurationKeyName("SUBDIVISIONS_AWS_SECRET_ACCESS_KEY")]
    public string? SubdivisionsAwsSecretKey { get; set; }
}

public class SubAwsCredentialsConfigOptions : IConfigureOptions<SubAwsCredentialsConfig>
{
    readonly IConfiguration? configuration;

    public SubAwsCredentialsConfigOptions(IConfiguration? configuration = null) => this.configuration = configuration;

    public void Configure(SubAwsCredentialsConfig options)
    {
        configuration?.Bind(options);
        options.SubdivisionsAwsAccessKey ??= Environment.GetEnvironmentVariable("SUBDIVISIONS_AWS_ACCESS_KEY_ID");
        options.SubdivisionsAwsSecretKey ??=
            Environment.GetEnvironmentVariable("SUBDIVISIONS_AWS_SECRET_ACCESS_KEY");
    }
}
