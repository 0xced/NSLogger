namespace NSLogger;

[SuppressMessage("ReSharper", "AutoPropertyCanBeMadeGetOnly.Global")]
public class LoggerConfiguration
{
    public const LoggerOptions DefaultOptions = LoggerOptions.BufferLogsUntilConnection |
                                                LoggerOptions.BrowseBonjour |
                                                LoggerOptions.BrowsePeerToPeer |
                                                LoggerOptions.BrowseOnlyLocalDomain |
                                                LoggerOptions.UseSsl;

    public LoggerOptions Options { get; init; } = DefaultOptions;

    public string? BonjourServiceName { get; init; } = null;

    public IPEndPoint? ViewerHost { get; init; } = null;

    public string? ClientName { get; init; } = Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyTitleAttribute>()?.Title;

    public string? ClientVersion { get; init; } = Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

    public bool WaitOnDispose { get; init; } = true;
}