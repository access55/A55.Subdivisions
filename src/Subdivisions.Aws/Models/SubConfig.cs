using Amazon;
using Amazon.Runtime;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Subdivisions.Models;

public class SubTopicNameConfig
{
    public string Suffix { get; set; } = "";
    public string Prefix { get; set; } = "a55";
    public string Source { get; set; } = "";
}

public class SubConfig : SubTopicNameConfig
{
    public int QueueMaxReceiveCount { get; set; } = 5;
    public int RetriesBeforeDeadLetter { get; set; } = 3;
    public string PubKey { get; set; } = "alias/PubSubKey";
    public int MessageRetentionInDays { get; set; } = 7;
    public int MessageTimeoutInSeconds { get; set; } = 30;
    public int MessageDelayInSeconds { get; set; }
    public double PollingIntervalInSeconds { get; set; } = 5;
    public string? ServiceUrl { get; set; }
    public bool Localstack { get; set; }
    public bool AutoCreateNewTopic { get; set; } = true;
    public string Region { get; set; } = "sa-east-1";
    public int LongPollingWaitInSeconds { get; set; }
}

public class ConfigureSubConfigOptions : IConfigureOptions<SubConfig>
{
    const string ConfigSection = "Subdivisions";
    readonly IConfiguration? configuration;
    readonly IHostEnvironment? hostEnvironment;

    public ConfigureSubConfigOptions(
        IConfiguration? configuration = null,
        IHostEnvironment? hostEnvironment = null)
    {
        this.hostEnvironment = hostEnvironment;
        this.configuration = configuration;
    }

    public void Configure(SubConfig options)
    {
        configuration?.GetSection(ConfigSection).Bind(options);
        if (hostEnvironment is not null && string.IsNullOrWhiteSpace(options.Source))
            options.Source = hostEnvironment.ApplicationName;

        if (Environment.GetEnvironmentVariable("SUBDIVISIONS_AWS_REGION") is { Length: > 0 } region)
            options.Region = region;
    }
}

sealed record SubAwsCredentialWrapper(AWSCredentials Credentials);
