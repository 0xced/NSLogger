using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace NSLoggerSharp;

public interface ILoggerConnector
{
    Stream Connect();

    Task<Stream> ConnectAsync(CancellationToken cancellationToken = default);
}