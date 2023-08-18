using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using ArkaneSystems.Arkane.Zeroconf;
using NSLoggerSharp.Zeroconf;

namespace NSLoggerSharp.Cli;

/// <summary>
/// Requires https://github.com/arkane-systems/Arkane.Zeroconf/pull/8 and https://github.com/arkane-systems/Arkane.Zeroconf/pull/9 to work properly
/// Also requires to install the Bonjour SDK for Windows v3.0 from https://developer.apple.com/bonjour/ → https://developer.apple.com/download/all/?q=Bonjour%20SDK%20for%20Windows
/// So that dnssd.dll is installed into C:\Windows\System32
/// Maybe installing Download Bonjour Print Services for Windows v2.0.2 could work too? http://support.apple.com/kb/DL999/
/// </summary>
public class ArkaneEnumerateConnector : LoggerConnector
{
    private readonly string? _serviceName;

    public ArkaneEnumerateConnector(string? serviceName) => _serviceName = serviceName;

    protected override async Task<IPEndPoint> GetEndPointAsync(CancellationToken cancellationToken = default)
    {
        using var browser = new ServiceBrowser();
        browser.Browse("_nslogger-ssl._tcp", "local");
        var stopwatch = Stopwatch.StartNew();
        while (stopwatch.Elapsed < TimeSpan.FromSeconds(5))
        {
            await Task.Delay(TimeSpan.FromMilliseconds(200), cancellationToken);
            foreach (var service in browser)
            {
                await service.ResolveAsync(cancellationToken);
                if (_serviceName == service.Name || (_serviceName == null && service.TxtRecord["filterClients"].ValueString != "1"))
                {
                    return new IPEndPoint(service.HostEntry.AddressList.First(), (ushort)service.Port);
                }
            }
        }

        throw new BonjourServiceNotFoundException();
    }
}