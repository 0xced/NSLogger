namespace NSLogger;

[Flags]
public enum LoggerOptions
{
    LogToConsole              = 1 << 0,
    BufferLogsUntilConnection = 1 << 1,
    BrowseBonjour             = 1 << 2,
    BrowseOnlyLocalDomain     = 1 << 3,
    UseSsl                    = 1 << 4,
    CaptureSystemConsole      = 1 << 5,
    BrowsePeerToPeer          = 1 << 6,
}