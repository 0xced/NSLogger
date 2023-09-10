using System;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace NSLoggerSharp;

public class LoggerConnector : ILoggerConnector
{
    private readonly IPEndPoint _endPoint;
    private Stream _stream = Stream.Null;
    private TcpClient? _client;

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
            _stream.Dispose();
            _stream = new DisposedStream();
            _client?.Dispose();
            _client = null;
        }
    }

    public virtual async ValueTask DisposeAsync()
    {
        await _stream.DisposeAsync();
        _stream = new DisposedStream();
        _client?.Dispose();
        _client = null;
    }

    public void Connect()
    {
        var remoteEndPoint = GetEndPoint();
        _client?.Dispose();
        _client = new TcpClient();
        _client.Connect(remoteEndPoint);
        var stream = _client.GetStream();
        _stream = stream;
        if (UseTls)
        {
            var tlsStream = new SslStream(stream, leaveInnerStreamOpen: false);
            tlsStream.AuthenticateAsClient(AuthenticationOptions);
            _stream = tlsStream;
        }
    }

    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        var remoteEndPoint = await GetEndPointAsync(cancellationToken);
        _client?.Dispose();
        _client = new TcpClient();
        await _client.ConnectAsync(remoteEndPoint, cancellationToken);
        var stream = _client.GetStream();
        _stream = stream;
        if (UseTls)
        {
            var tlsStream = new SslStream(stream, leaveInnerStreamOpen: false);
            await tlsStream.AuthenticateAsClientAsync(AuthenticationOptions, cancellationToken);
            _stream = tlsStream;
        }
    }

    public void Write(ReadOnlySpan<byte> messageData)
    {
        _stream.Write(messageData);
        _stream.Flush();
    }

    public async ValueTask WriteAsync(ReadOnlyMemory<byte> messageData, CancellationToken cancellationToken)
    {
        await _stream.WriteAsync(messageData, cancellationToken);
        await _stream.FlushAsync(cancellationToken);
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