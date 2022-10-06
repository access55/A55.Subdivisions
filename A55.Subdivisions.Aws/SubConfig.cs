namespace A55.Subdivisions.Aws;

public class SubConfig
{
    public int QueueMaxReceiveCount { get; set; } = 1000;
    public string? PubKey { get; set; }
}