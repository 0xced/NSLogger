using System;
using System.Threading;
using System.Threading.Tasks;

namespace NSLoggerSharp;

public interface ILoggerConnector : IDisposable, IAsyncDisposable
{
    void Connect();

    Task ConnectAsync(CancellationToken cancellationToken = default);

    void Write(ReadOnlySpan<byte> messageData);

    ValueTask WriteAsync(ReadOnlyMemory<byte> messageData, CancellationToken cancellationToken);
}