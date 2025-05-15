namespace IMSystem.Server.Infrastructure.Configuration;

public class FileCleanupSettings
{
    public int IntervalHours { get; set; }
    public int UnconfirmedUploadTimeoutHours { get; set; }
}