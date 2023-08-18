using System;
using static BonjourSharp.DnsServiceDiscovery;

namespace BonjourSharp;

public struct DnsServiceRef : IDisposable
{
#pragma warning disable CS0649
    // Unassigned readonly field => actually assigned through native PInvoke calls
    private readonly IntPtr _ptr;
#pragma warning restore CS0649

    public void Dispose()
    {
        DnsServiceRefDeallocate(this);
    }

    public void Process()
    {
        var error = DnsServiceProcessResult(this);
        DnsServiceDiscoveryException.ThrowIfError(error, nameof(DnsServiceProcessResult));
    }

    public int SocketFileDescriptor => DnsServiceRefSockFd(this);

    public override bool Equals(object? o)
    {
        if (o is not DnsServiceRef serviceRef)
            return false;

        return serviceRef._ptr == _ptr;
    }

    public override int GetHashCode() => _ptr.GetHashCode();

    public static bool operator ==(DnsServiceRef a, DnsServiceRef b) => a._ptr == b._ptr;

    public static bool operator !=(DnsServiceRef a, DnsServiceRef b) => a._ptr != b._ptr;
}