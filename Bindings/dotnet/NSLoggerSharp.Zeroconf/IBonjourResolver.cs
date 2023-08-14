using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace NSLoggerSharp.Zeroconf;

public interface IBonjourResolver
{
    Task<IPEndPoint> ResolveBonjourServiceAsync(string? serviceName, CancellationToken cancellationToken);
}