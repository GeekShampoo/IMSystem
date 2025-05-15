namespace IMSystem.Server.Infrastructure.Configuration;

public class OutboxProcessorSettings
{
    public int PollingIntervalSeconds { get; set; }
    public int MaxRetryCount { get; set; }
    public int BatchSize { get; set; }
}