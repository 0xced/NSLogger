using System;
using System.Threading;
using System.Threading.Tasks;

namespace NSLoggerSharp;

public interface ILoggerConnection : IDisposable, IAsyncDisposable
{
    void Send(ReadOnlySpan<byte> messageData);

    ValueTask SendAsync(ReadOnlyMemory<byte> messageData, CancellationToken cancellationToken);
}