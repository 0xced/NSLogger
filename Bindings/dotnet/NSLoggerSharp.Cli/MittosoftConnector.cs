using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Mittosoft.DnsServiceDiscovery;
using NSLoggerSharp.Zeroconf;
using ResolveEventArgs = Mittosoft.DnsServiceDiscovery.ResolveEventArgs;

namespace NSLoggerSharp.Cli;

/// <summary>
/// Requires the Bonjour Service to be running on Windows!
/// </summary>
public class MittosoftConnector : LoggerConnector
{
    private readonly string? _serviceName;

    public MittosoftConnector(string? serviceName) => _serviceName = serviceName;

    protected override async Task<IPEndPoint> GetEndPointAsync(CancellationToken cancellationToken = default)
    {
        var discoverer = new Discoverer();
        var stopwatch = Stopwatch.StartNew();

        await foreach (var result in discoverer.BrowseAsync("_nslogger-ssl._tcp", "local", cancellationToken))
        {
            var serviceName = result.HostName;
            if (_serviceName == serviceName || (_serviceName == null /*&& resolveArg.TxtRecord["filterClients"].ValueString != "1"*/))
            {
                return new IPEndPoint(result.IPAddress, 0);
            }

            if (stopwatch.Elapsed > TimeSpan.FromSeconds(5))
            {
                break;
            }
        }

        throw new BonjourServiceNotFoundException();
    }

    private class Discoverer
    {
        private readonly DnsServiceDiscovery _discovery;
        private readonly Channel<LookupEventArgs> _channel;

        public Discoverer()
        {
            _discovery = new DnsServiceDiscovery();
            _discovery.BrowseEvent += DiscoveryOnBrowseEvent;
            _discovery.ResolveEvent += DiscoveryOnResolveEvent;
            _discovery.LookupEvent += DiscoveryOnLookupEvent;
            _channel = Channel.CreateUnbounded<LookupEventArgs>();
        }

        private async void DiscoveryOnBrowseEvent(object? sender, BrowseEventArgs e)
        {
            if (e.EventType == BrowseEventType.Added)
            {
                await _discovery.ResolveAsync(e.Descriptor);
            }
        }

        private async void DiscoveryOnResolveEvent(object? sender, ResolveEventArgs e)
        {
            await _discovery.LookupAsync(e.HostName, ProtocolFlags.IPv4v6, withTimeout: true, interfaceIndex: e.InterfaceIndex);
        }

        private async void DiscoveryOnLookupEvent(object? sender, LookupEventArgs e)
        {
            if (e.EventType == LookupEventType.Added)
            {
                await _channel.Writer.WriteAsync(e);
            }
        }

        public async IAsyncEnumerable<LookupEventArgs> BrowseAsync(string service, string domain, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await _discovery.BrowseAsync(service, domain);
            await foreach (var result in _channel.Reader.ReadAllAsync(cancellationToken))
            {
                yield return result;
            }
        }
    }
}