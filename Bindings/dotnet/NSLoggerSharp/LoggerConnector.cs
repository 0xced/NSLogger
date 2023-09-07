using System;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace NSLoggerSharp;

public class LoggerConnector : ILoggerConnector, IDisposable
{
    private readonly IPEndPoint _endPoint;
    private readonly TcpClient _client = new();

    protected virtual bool UseTls { get; } = true;

    public LoggerConnector() : this(new IPEndPoint(IPAddress.Loopback, 50000))
    {
    }

    public LoggerConnector(IPEndPoint endPoint, bool useTls = true)
    {
        _endPoint = endPoint ?? throw new ArgumentNullException(nameof(endPoint));
        UseTls = useTls;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _client.Dispose();
        }
    }

    public Stream Connect()
    {
        var remoteEndPoint = GetEndPoint();
        _client.Connect(remoteEndPoint);
        var stream = _client.GetStream();
        if (UseTls)
        {
            var tlsStream = new SslStream(stream, leaveInnerStreamOpen: false);
            tlsStream.AuthenticateAsClient(AuthenticationOptions);
            return tlsStream;
        }
        return stream;
    }

    public async Task<Stream> ConnectAsync(CancellationToken cancellationToken = default)
    {
        var remoteEndPoint = await GetEndPointAsync(cancellationToken);
        await _client.ConnectAsync(remoteEndPoint, cancellationToken);
        var stream = _client.GetStream();
        if (UseTls)
        {
            var tlsStream = new SslStream(stream, leaveInnerStreamOpen: false);
            await tlsStream.AuthenticateAsClientAsync(AuthenticationOptions, cancellationToken);
            return tlsStream;
        }
        return stream;
    }

    protected virtual IPEndPoint GetEndPoint() => GetEndPointAsync().GetAwaiter().GetResult();

    protected virtual Task<IPEndPoint> GetEndPointAsync(CancellationToken cancellationToken = default) => Task.FromResult(_endPoint);

    public virtual SslClientAuthenticationOptions AuthenticationOptions { get; } = new()
    {
        // https://github.com/fpillet/NSLogger/blob/e8c453142da7051462cca189d3fcee74de0500ea/Desktop/Resources/NSLoggerCertReq.conf#L10
        RemoteCertificateValidationCallback = (_, certificate, _, _) => certificate?.Issuer.Contains("NSLogger self-signed SSL") ?? false,
    };
}