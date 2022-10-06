namespace A55.Subdivisions.Aws;

public class SubConfig
{
    public int QueueMaxReceiveCount { get; set; } = 1000;
    public string PubKey { get; set; } = "alias/PubSubKey";
    public int MessageRetantionInDays { get; set; } = 7;
    public int MessageTimeoutInSeconds { get; set; } = 30;
    public int MessageDelayInSeconds { get; set; } = 0;
    public string? Source { get; set; }
    public string Sufix { get; set; } = "";
    public string Prefix { get; set; } = "a55";
    public bool AutoCreateNewTopic { get; set; } = true;
}
