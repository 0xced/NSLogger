using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace NSLoggerSharp;

internal sealed class LoggerConnection : ILoggerConnection
{
    private readonly TcpClient _tcpClient;
    private Stream _stream;

    public LoggerConnection(TcpClient tcpClient, Stream stream)
    {
        _tcpClient = tcpClient ?? throw new ArgumentNullException(nameof(tcpClient));
        _stream = stream ?? throw new ArgumentNullException(nameof(stream));
    }

    public void Dispose()
    {
        _stream.Dispose();
        _stream = new DisposedStream();
        _tcpClient.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        await _stream.DisposeAsync();
        _stream = new DisposedStream();
        _tcpClient.Dispose();
    }

    public void Send(ReadOnlySpan<byte> messageData)
    {
        _stream.Write(messageData);
        _stream.Flush();
    }

    public async ValueTask SendAsync(ReadOnlyMemory<byte> messageData, CancellationToken cancellationToken)
    {
        await _stream.WriteAsync(messageData, cancellationToken);
        await _stream.FlushAsync(cancellationToken);
    }
}