namespace A55.Subdivisions.Aws;

public class SubConfig
{
    public int QueueMaxReceiveCount { get; set; } = 1000;
    public string? PubKey { get; set; }
    public int MessageRetantionInDays { get; set; } = 7;
    public int MessageTimeoutInSeconds { get; set; } = 30;
    public int MessageDelayInSeconds { get; set; }
}
