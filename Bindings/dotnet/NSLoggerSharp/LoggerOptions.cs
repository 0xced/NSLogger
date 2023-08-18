using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace NSLoggerSharp;

[SuppressMessage("ReSharper", "AutoPropertyCanBeMadeGetOnly.Global")]
public class LoggerOptions
{
    public string? ClientName { get; init; } = Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyTitleAttribute>()?.Title;

    public string? ClientVersion { get; init; } = Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                                                  ?? Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyVersionAttribute>()?.Version;

    public string? ClientModel { get; init; } = null;

    public string? OsName { get; init; } = GetOsName();

    public string? OsVersion { get; init; } = Environment.OSVersion.Version.ToString();

    private static string GetOsName()
    {
        if (OperatingSystem.IsAndroid()) return "Android";
        if (OperatingSystem.IsBrowser()) return "Browser";
        if (OperatingSystem.IsFreeBSD()) return "FreeBSD";
        if (OperatingSystem.IsIOS()) return "iOS";
        if (OperatingSystem.IsLinux()) return "Linux";
        if (OperatingSystem.IsMacOS() || OperatingSystem.IsMacCatalyst()) return "macOS";
        if (OperatingSystem.IsTvOS()) return "tvOS";
        if (OperatingSystem.IsWatchOS()) return "watchOS";
        if (OperatingSystem.IsWindows()) return "Windows";

        return "Unknown OS";
    }
}