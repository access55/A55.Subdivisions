using Amazon;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace A55.Subdivisions.Aws.Models;

public class SubTopicNameConfig
{
    public string Suffix { get; set; } = "";
    public string Prefix { get; set; } = "a55";
    public string? Source { get; set; }
    internal string? FallbackSource { get; private set; }
    public void SetFallbackSource(string fallback) => FallbackSource = fallback;
}

public class SubConfig : SubTopicNameConfig
{
    public int QueueMaxReceiveCount { get; set; } = 5;
    public int RetriesBeforeDeadLetter { get; set; } = 3;
    public string PubKey { get; set; } = "alias/PubSubKey";
    public int MessageRetantionInDays { get; set; } = 7;
    public int MessageTimeoutInSeconds { get; set; } = 30;
    public int MessageDelayInSeconds { get; set; }

    public int PollingIntervalInSeconds { get; set; } = 5;
    public string? ServiceUrl { get; set; }
    public bool AutoCreateNewTopic { get; set; } = true;
    public string Region { get; set; } = "sa-east-1";
    public RegionEndpoint Endpoint => RegionEndpoint.GetBySystemName(Region);
    public int WaitMessageInSeconds { get; set; }
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

        if (hostEnvironment is not null && options.FallbackSource is null)
            options.SetFallbackSource(hostEnvironment.ApplicationName);
    }
}
