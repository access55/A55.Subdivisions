using Amazon;

namespace A55.Subdivisions.Aws.Models;

public class SubTopicNameConfig
{
    public string? Source { get; set; }
    public string Suffix { get; set; } = "";
    public string Prefix { get; set; } = "a55";
}

public class SubConfig : SubTopicNameConfig
{
    public int QueueMaxReceiveCount { get; set; } = 1000;
    public string PubKey { get; set; } = "alias/PubSubKey";
    public int MessageRetantionInDays { get; set; } = 7;
    public int MessageTimeoutInSeconds { get; set; } = 30;
    public int MessageDelayInSeconds { get; set; }
    public string? ServiceUrl { get; set; }
    public bool AutoCreateNewTopic { get; set; } = true;
    public string Region { get; set; } = "sa-east-1";
    public RegionEndpoint Endpoint => RegionEndpoint.GetBySystemName(Region);
}
