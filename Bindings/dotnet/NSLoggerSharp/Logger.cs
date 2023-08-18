using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace NSLoggerSharp;

public sealed class Logger : IDisposable, IAsyncDisposable
{
    private readonly ILoggerConnector _connector;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly LoggerOptions _options;
    private Stream _stream = Stream.Null;
    private TcpClient? _tcpClient;
    private int _seq;

    public Logger() : this(new LoggerConnector())
    {
    }

    public Logger(ILoggerConnector connector, LoggerOptions? options = null)
    {
        _connector = connector ?? throw new ArgumentNullException(nameof(connector));
        _options = options ?? new LoggerOptions();
    }

    public void Connect()
    {
        CriticalSection(() => ConnectInternalAsync(async: false).GetAwaiter().GetResult());
    }

    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        await CriticalSectionAsync(async ct => await ConnectInternalAsync(async: true, ct), cancellationToken);
    }

    [SuppressMessage("ReSharper", "MethodHasAsyncOverloadWithCancellation")]
    private async Task ConnectInternalAsync(bool async, CancellationToken cancellationToken = default)
    {
        if (async)
            _tcpClient = await _connector.ConnectAsync(cancellationToken);
        else
            _tcpClient = _connector.Connect();

        var sslStream = new SslStream(_tcpClient.GetStream(), leaveInnerStreamOpen: false);

        _stream = sslStream;

        if (async)
        {
            await sslStream.AuthenticateAsClientAsync(_connector.AuthenticationOptions, cancellationToken);
            await LogInternalAsync(new Message.ClientInfo(_options), async: true, cancellationToken);
        }
        else
        {
            sslStream.AuthenticateAsClient(_connector.AuthenticationOptions);
            LogInternalAsync(new Message.ClientInfo(_options), async: false, cancellationToken).GetAwaiter().GetResult();
        }
    }

    public void Log(Message message)
    {
        CriticalSection(message, m => LogInternalAsync(m, async: false).GetAwaiter().GetResult());
    }

    public async Task LogAsync(Message message, CancellationToken cancellationToken = default)
    {
        await CriticalSectionAsync(message, async (m, ct) => await LogInternalAsync(m, async: true, ct), cancellationToken);
    }

    [SuppressMessage("ReSharper", "MethodHasAsyncOverloadWithCancellation")]
    private async Task LogInternalAsync(Message message, bool async, CancellationToken cancellationToken = default)
    {
        if (_stream == Stream.Null)
        {
            if (async)
                await ConnectAsync(cancellationToken);
            else
                Connect();
        }

        // TODO: writing to the stream might fail => the connection should be automatically retried and messages buffered
        // See https://github.com/serilog-contrib/Serilog.Sinks.Network/blob/ce131dcea588d959f80e06965586dd5d35e6371a/Serilog.Sinks.Network/Sinks/TCP/TCPSocketWriter.cs#L31-L48 for inspiration
        if (async)
        {
            await _stream.WriteAsync(message, _seq++, cancellationToken);
            await _stream.FlushAsync(cancellationToken);
        }
        else
        {
            _stream.Write(message, _seq++);
            _stream.Flush();
        }
    }

    public void Dispose()
    {
        CriticalSection(() =>
        {
            _stream.Dispose();
            _stream = new DisposedStream();
            _tcpClient?.Dispose();
            _tcpClient = null;
        });
        _semaphore.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        await CriticalSectionAsync(async _ =>
        {
            await _stream.DisposeAsync();
            _stream = new DisposedStream();
            _tcpClient?.Dispose();
            _tcpClient = null;
        }, CancellationToken.None);
        _semaphore.Dispose();
    }

    private void CriticalSection(Action action)
    {
        CriticalSection<object>(default!, _ => action());
    }

    private void CriticalSection<T>(T argument, Action<T> action)
    {
        _semaphore.Wait();
        try
        {
            action(argument);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task CriticalSectionAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken)
    {
        await CriticalSectionAsync<object>(default!, async (_, ct) => await action(ct), cancellationToken);
    }

    private async Task CriticalSectionAsync<T>(T argument, Func<T, CancellationToken, Task> action, CancellationToken cancellationToken)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            await action(argument, cancellationToken);
        }
        finally
        {
            _semaphore.Release();
        }
    }
}