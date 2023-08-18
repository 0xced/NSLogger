using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using ArkaneSystems.Arkane.Zeroconf;

namespace NSLoggerSharp.Cli;

/// <summary>
/// Requires https://github.com/arkane-systems/Arkane.Zeroconf/pull/8 and https://github.com/arkane-systems/Arkane.Zeroconf/pull/9 to work properly
/// Also requires to install the Bonjour SDK for Windows v3.0 from https://developer.apple.com/bonjour/ → https://developer.apple.com/download/all/?q=Bonjour%20SDK%20for%20Windows
/// So that dnssd.dll is installed into C:\Windows\System32
/// Maybe installing Download Bonjour Print Services for Windows v2.0.2 could work too? http://support.apple.com/kb/DL999/
/// </summary>
public class ArkaneConnector : LoggerConnector
{
    private readonly ServiceBrowser _browser;
    private string? _serviceName;
    private TaskCompletionSource<IPEndPoint>? _completionSource;

    public ArkaneConnector(string? serviceName)
    {
        _serviceName = serviceName;
        _browser = new ServiceBrowser();
        _browser.ServiceAdded += OnServiceAdded;
    }

    protected override async Task<IPEndPoint> GetEndPointAsync(CancellationToken cancellationToken = default)
    {
        if (_completionSource != null)
        {
            throw new InvalidOperationException();
        }

        // TODO: handle _serviceName and also TxtRecord with filterClients=1

        _completionSource = new TaskCompletionSource<IPEndPoint>();
        cancellationToken.Register(() =>
        {
            _completionSource.SetCanceled(cancellationToken);
            _completionSource = null;
        });

        _browser.Browse("_nslogger-ssl._tcp", "local");

        return await _completionSource.Task;
    }

    private void OnServiceAdded(object sender, ServiceBrowseEventArgs args)
    {
        _browser.ServiceAdded -= OnServiceAdded;
        args.Service.Resolved += OnServiceResolved;
        args.Service.Resolve();
    }

    private void OnServiceResolved(object sender, ServiceResolvedEventArgs args)
    {
        args.Service.Resolved -= OnServiceResolved;
        _completionSource!.SetResult(new IPEndPoint(args.Service.HostEntry.AddressList.First(), (ushort)args.Service.Port));
        _completionSource = null;
    }
}