using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using ArkaneSystems.Arkane.Zeroconf;
using NSLoggerSharp.Zeroconf;

namespace NSLoggerSharp.Cli;

/// <summary>
/// Requires to patch the BrowseService class to avoid PlatformNotSupportedException (https://stackoverflow.com/questions/45183294/begininvoke-not-supported-on-net-core-platformnotsupported-exception/55516918#55516918)
/// From: this.resolveResult = this.resolveAction.BeginInvoke (false, null, null) ;
/// To:   this.resolveResult = Task.Run(() => this.resolveAction(false)) ;
/// Also requires to install the Bonjour SDK for Windows v3.0 from https://developer.apple.com/bonjour/ → https://developer.apple.com/download/all/?q=Bonjour%20SDK%20for%20Windows
/// So that dnssd.dll is installed into C:\Windows\System32
/// Maybe installing Download Bonjour Print Services for Windows v2.0.2 could work too? https://support.apple.com/kb/DL999?locale=en_US
/// </summary>
public class ArkaneResolver : IBonjourResolver
{
    private readonly ServiceBrowser _browser;
    private string? _serviceName;
    private TaskCompletionSource<IPEndPoint>? _completionSource;

    public ArkaneResolver()
    {
        _browser = new ServiceBrowser();
        _browser.ServiceAdded += OnServiceAdded;
    }

    public async Task<IPEndPoint> ResolveBonjourServiceAsync(string? serviceName, CancellationToken cancellationToken)
    {
        if (_completionSource != null)
        {
            throw new InvalidOperationException();
        }

        // TODO: handle _serviceName and also TxtRecord with filterClients=1
        _serviceName = serviceName;

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