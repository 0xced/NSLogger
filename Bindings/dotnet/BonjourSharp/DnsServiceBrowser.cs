using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using static BonjourSharp.DnsServiceDiscovery;

namespace BonjourSharp;

public record BonjourService(string Name)
{
    private readonly List<IPAddress> _addresses = new(8);

    public string HostName { get; internal set; } = "";

    public string FullName { get; internal set; } = "";

    public ushort Port { get; internal set; }

    public IReadOnlyCollection<IPAddress> Addresses => _addresses;

    internal void AddAddress(IPAddress address) => _addresses.Add(address);

    public override string ToString() => $"{Name} ({HostName}) [{FullName}] {Environment.NewLine}{string.Join(Environment.NewLine, _addresses.Select(e => $"  🌐 {new IPEndPoint(e, Port)}"))}";
}

// TODO: maybe also take inspiration from https://developer.apple.com/documentation/network/nwbrowser ?
public static class DnsServiceBrowser
{
    public static async IAsyncEnumerable<BonjourService> BrowseAsync(string serviceType, string? domain = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var channel = Channel.CreateUnbounded<BonjourService>(new UnboundedChannelOptions { SingleReader = true, SingleWriter = true });
        var context = new BrowseContext(channel, cancellationToken);

        var error = DnsServiceBrowse(out var sdRef, flags: DnsServiceFlags.None, interfaceIndex: 0, serviceType, domain, callback: OnServiceBrowseReply, context);
        DnsServiceDiscoveryException.ThrowIfError(error, nameof(DnsServiceBrowse));
        cancellationToken.Register(() => sdRef.Dispose());
        _ = Task.Run(() =>
        {
            while (DnsServiceProcessResult(sdRef) == DnsServiceError.NoError)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }
        }, cancellationToken);

        await foreach (var service in channel.Reader.ReadAllAsync(cancellationToken))
        {
            yield return service;
        }

        // TODO: Never reached because the channel is never completed.
        // Should it be completed after some (user provided) timeout or should we use the cancellationToken exclusively to exit this method?
    }

    private static void OnServiceBrowseReply(DnsServiceRef sdRef, DnsServiceFlags flags, uint interfaceIndex, DnsServiceError errorCode, string serviceName, string regType, string replyDomain, IntPtr context)
    {
        Console.WriteLine("+++ OnServiceBrowseReply");
        var browseContext = GCHandle.FromIntPtr(context).Target as BrowseContext ?? throw new ArgumentException($"The context must hold a {nameof(BrowseContext)} instance.", nameof(context));

        if (flags.HasFlag(DnsServiceFlags.Add))
        {
            browseContext.CurrentServiceName = serviceName;
            if (!browseContext.BonjourServices.TryGetValue(serviceName, out var bonjourService))
            {
                bonjourService = new BonjourService(serviceName);
                browseContext.BonjourServices[serviceName] = bonjourService;
            }

            browseContext.CancellationToken.ThrowIfCancellationRequested();

            var error = DnsServiceResolve(out var resolveRef, DnsServiceFlags.None, interfaceIndex, serviceName, regType, replyDomain, OnServiceResolveReply, context);
            DnsServiceDiscoveryException.ThrowIfError(error, nameof(DnsServiceResolve));
            resolveRef.Process();
            resolveRef.Dispose();

            Console.WriteLine($"➕ {serviceName} ({flags})");
            //browseContext.Channel.Writer.TryWrite(bonjourService);
            //Thread.Sleep(100);
        }
        else
        {
            browseContext.CurrentServiceName = "";
            browseContext.BonjourServices.Remove(serviceName);
            // TODO: another object that represents a removed Bonjour service
            Console.WriteLine($"➖ {serviceName} ({flags})");
            browseContext.Channel.Writer.TryWrite(new BonjourService($"REMOVED {serviceName}"));
        }

        if (!flags.HasFlag(DnsServiceFlags.MoreComing))
        {
            foreach (var bonjourService in browseContext.BonjourServices.Values)
            {
                browseContext.Channel.Writer.TryWrite(bonjourService);
            }
            browseContext.BonjourServices.Clear();
        }
    }

    private static unsafe void OnServiceResolveReply(DnsServiceRef sdRef, DnsServiceFlags flags, uint interfaceIndex, DnsServiceError errorCode, string fullName, string hostTarget, ushort port, ushort txtLen, IntPtr txtRecord, IntPtr context)
    {
        Console.WriteLine(" +++ OnServiceResolveReply");
        var browseContext = GCHandle.FromIntPtr(context).Target as BrowseContext ?? throw new ArgumentException($"The context must hold a {nameof(BrowseContext)} instance.", nameof(context));
        var bonjourService = browseContext.BonjourServices[browseContext.CurrentServiceName];
        bonjourService.HostName = hostTarget;
        bonjourService.FullName = fullName;
        bonjourService.Port = port;
        // TODO parse the TXT record too
        browseContext.CancellationToken.ThrowIfCancellationRequested();

        foreach (var serviceType in new[] { DnsServiceType.A, DnsServiceType.AAAA })
        {
            var error = DnsServiceQueryRecord(out var queryRef, DnsServiceFlags.None, interfaceIndex, hostTarget, serviceType, DnsServiceClass.IN, OnQueryRecordReply, context);
            DnsServiceDiscoveryException.ThrowIfError(error, nameof(DnsServiceResolve));
            queryRef.Process();
            queryRef.Dispose();
        }
    }

    private static unsafe void OnQueryRecordReply(DnsServiceRef sdRef, DnsServiceFlags flags, uint interfaceIndex, DnsServiceError errorCode, string fullName, DnsServiceType rrType, DnsServiceClass rrClass, ushort rdLen, byte* rData, uint ttl, IntPtr context)
    {
        Console.WriteLine("  +++ OnQueryRecordReply");
        var browseContext = GCHandle.FromIntPtr(context).Target as BrowseContext ?? throw new ArgumentException($"The context must hold a {nameof(BrowseContext)} instance.", nameof(context));
        if (rrType is DnsServiceType.A or DnsServiceType.AAAA)
        {
            var data = new ReadOnlySpan<byte>(rData, rdLen);
            var address = new IPAddress(data);
            var bonjourService = browseContext.BonjourServices[browseContext.CurrentServiceName];
            Console.WriteLine($"({flags}) {address} on {NetworkInterface.GetAllNetworkInterfaces()[interfaceIndex - 1].Name}");
            bonjourService.AddAddress(address);
        }
    }

    private class BrowseContext
    {
        private readonly GCHandle _handle;

        public BrowseContext(Channel<BonjourService> channel, CancellationToken cancellationToken)
        {
            Channel = channel;
            CancellationToken = cancellationToken;
            _handle = GCHandle.Alloc(this);
        }

        public static implicit operator IntPtr(BrowseContext browseContext) => GCHandle.ToIntPtr(browseContext._handle);

        public Channel<BonjourService> Channel { get; }
        public CancellationToken CancellationToken { get; }
        public string CurrentServiceName { get; set; } = "";
        public Dictionary<string, BonjourService> BonjourServices { get; } = new();
    }
}