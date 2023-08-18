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
/// Requires https://github.com/0xced/Arkane.Zeroconf/tree/BrowseAsync
/// </summary>
public class ArkaneBrowseConnector : LoggerConnector
{
    private readonly string? _serviceName;

    public ArkaneBrowseConnector(string? serviceName) => _serviceName = serviceName;

    protected override async Task<IPEndPoint> GetEndPointAsync(CancellationToken cancellationToken = default)
    {
        using var browser = new ServiceBrowser();
        var stopwatch = Stopwatch.StartNew();
#if false
        await foreach (var service in browser.BrowseAsync("_nslogger-ssl._tcp", "local", cancellationToken))
        {
            if (_serviceName == service.Name || (_serviceName == null && service.TxtRecord["filterClients"].ValueString != "1"))
            {
                return new IPEndPoint(service.HostEntry.AddressList.First(), (ushort)service.Port);
            }

            if (stopwatch.Elapsed > TimeSpan.FromSeconds(5))
            {
                break;
            }
        }
#endif
        throw new BonjourServiceNotFoundException();
    }
}