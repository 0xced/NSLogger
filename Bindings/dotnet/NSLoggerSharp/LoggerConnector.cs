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

    protected virtual bool UseTls { get; } = true;

    public LoggerConnector() : this(new IPEndPoint(IPAddress.Loopback, 50000))
    {
    }

    public LoggerConnector(IPEndPoint endPoint, bool useTls = true)
    {
        _endPoint = endPoint ?? throw new ArgumentNullException(nameof(endPoint));
        UseTls = useTls;
    }

    public ILoggerConnection Connect()
    {
        var remoteEndPoint = GetEndPoint();
        var client = new TcpClient();
        client.Connect(remoteEndPoint);
        var stream = client.GetStream();
        if (UseTls)
        {
            var tlsStream = new SslStream(stream, leaveInnerStreamOpen: false);
            tlsStream.AuthenticateAsClient(AuthenticationOptions);
            return new LoggerConnection(client, tlsStream);
        }
        return new LoggerConnection(client, stream);
    }

    public async Task<ILoggerConnection> ConnectAsync(CancellationToken cancellationToken = default)
    {
        var remoteEndPoint = await GetEndPointAsync(cancellationToken);
        var client = new TcpClient();
        await client.ConnectAsync(remoteEndPoint, cancellationToken);
        var stream = client.GetStream();
        if (UseTls)
        {
            var tlsStream = new SslStream(stream, leaveInnerStreamOpen: false);
            await tlsStream.AuthenticateAsClientAsync(AuthenticationOptions, cancellationToken);
            return new LoggerConnection(client, tlsStream);
        }
        return new LoggerConnection(client, stream);
    }

    protected virtual IPEndPoint GetEndPoint() => GetEndPointAsync().GetAwaiter().GetResult();

    protected virtual Task<IPEndPoint> GetEndPointAsync(CancellationToken cancellationToken = default) => Task.FromResult(_endPoint);

    public virtual SslClientAuthenticationOptions AuthenticationOptions { get; } = new()
    {
        // Throws ArgumentNullException if null on .NET 6 (can be null on .NET 7 onwards)
        // See https://github.com/dotnet/runtime/blob/v6.0.21/src/libraries/System.Net.Security/src/System/Net/Security/SslStream.Implementation.cs#L71-L74)
        TargetHost = "",
        // https://github.com/fpillet/NSLogger/blob/e8c453142da7051462cca189d3fcee74de0500ea/Desktop/Resources/NSLoggerCertReq.conf#L10
        RemoteCertificateValidationCallback = (_, certificate, _, _) => certificate?.Issuer.Contains("NSLogger self-signed SSL") ?? false,
    };
}