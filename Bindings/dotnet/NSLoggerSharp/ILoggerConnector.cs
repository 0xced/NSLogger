using System.Threading;
using System.Threading.Tasks;

namespace NSLoggerSharp;

public interface ILoggerConnector
{
    ILoggerConnection Connect();

    Task<ILoggerConnection> ConnectAsync(CancellationToken cancellationToken = default);
}