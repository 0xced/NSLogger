using System.Net.Security;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace NSLoggerSharp;

public interface ILoggerConnector
{
    TcpClient Connect();

    Task<TcpClient> ConnectAsync(CancellationToken cancellationToken = default);

    SslClientAuthenticationOptions AuthenticationOptions { get; }
}