using System;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace NSLoggerSharp;

public class LoggerConnector : ILoggerConnector
{
    private readonly IPEndPoint _endPoint;

    public LoggerConnector() : this(new IPEndPoint(IPAddress.Loopback, 50000))
    {
    }

    public LoggerConnector(IPEndPoint endPoint)
    {
        _endPoint = endPoint ?? throw new ArgumentNullException(nameof(endPoint));
    }

    public TcpClient Connect()
    {
        var remoteEndPoint = GetEndPoint();
        var client = new TcpClient();
        client.Connect(remoteEndPoint);
        return client;
    }

    public async Task<TcpClient> ConnectAsync(CancellationToken cancellationToken = default)
    {
        var remoteEndPoint = await GetEndPointAsync(cancellationToken);
        var client = new TcpClient();
        await client.ConnectAsync(remoteEndPoint, cancellationToken);
        return client;
    }

    protected virtual IPEndPoint GetEndPoint() => GetEndPointAsync().GetAwaiter().GetResult();

    protected virtual Task<IPEndPoint> GetEndPointAsync(CancellationToken cancellationToken = default) => Task.FromResult(_endPoint);

    public virtual SslClientAuthenticationOptions AuthenticationOptions { get; } = new()
    {
        // https://github.com/fpillet/NSLogger/blob/e8c453142da7051462cca189d3fcee74de0500ea/Desktop/Resources/NSLoggerCertReq.conf#L10
        RemoteCertificateValidationCallback = (_, certificate, _, _) => certificate?.Issuer.Contains("NSLogger self-signed SSL") ?? false,
    };
}