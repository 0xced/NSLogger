using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Zeroconf;

namespace NSLoggerSharp.Zeroconf;

public class BonjourResolver : IBonjourResolver
{
    public async Task<IPEndPoint> ResolveBonjourServiceAsync(string? serviceName, CancellationToken cancellationToken)
    {
        using var source = new CancellationTokenSource();
        using var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(source.Token, cancellationToken);
        var exceptions = new List<Exception>();
        IPEndPoint? viewerHost = null;
        try
        {
            await ZeroconfResolver.ResolveAsync("_nslogger-ssl._tcp.local.", scanTime: TimeSpan.FromSeconds(10), callback: host =>
            {
                try
                {
                    var service = GetService(host, serviceName);
                    viewerHost = new IPEndPoint(IPAddress.Parse(host.IPAddress), service.Port);
                    source.Cancel();
                }
                catch (BonjourServiceNotFoundException exception)
                {
                    exceptions.Add(exception);
                }
            }, cancellationToken: linkedSource.Token);
        }
        catch (OperationCanceledException) when (source.IsCancellationRequested)
        {
            // Early abort (before reaching scanTime)
        }

        if (viewerHost != null)
            return viewerHost;

        throw exceptions.Count switch
        {
            0 => new BonjourServiceNotFoundException(),
            1 => exceptions[0],
            _ => new AggregateException(exceptions)
        };
    }

    private static IService GetService(IZeroconfHost host, string? serviceName)
    {
        if (serviceName != null)
        {
            if (host.Services.TryGetValue($"{serviceName}._nslogger-ssl._tcp.local.", out var service))
                return service;

            throw new BonjourServiceNotFoundException(host, serviceName);
        }

        var unfilteredServices = host.Services.Values.Where(e => !e.FilterClients()).ToList();
        return unfilteredServices.Count switch
        {
            0 => throw new BonjourServiceNotFoundException(host),
            1 => unfilteredServices[0],
            _ => throw new BonjourServiceNotFoundException(host, unfilteredServices)
        };
    }
}