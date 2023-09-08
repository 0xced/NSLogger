using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace NSLoggerSharp;

public sealed class Logger : IDisposable, IAsyncDisposable
{
    private readonly ILoggerConnector _connector;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly LoggerOptions _options;
    private Stream _stream = Stream.Null;
    private int _seq;

    public Logger() : this(new LoggerConnector(), new LoggerOptions())
    {
    }

    public Logger(ILoggerConnector connector) : this(connector, new LoggerOptions())
    {
    }

    public Logger(LoggerOptions options) : this(new LoggerConnector(), options)
    {
    }

    public Logger(ILoggerConnector connector, LoggerOptions options)
    {
        _connector = connector ?? throw new ArgumentNullException(nameof(connector));
        _options = options ?? throw new ArgumentNullException(nameof(options));
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
        {
            _stream = await _connector.ConnectAsync(cancellationToken);
            await LogInternalAsync(new Message.ClientInfo(_options), async: true, cancellationToken);
        }
        else
        {
            _stream = _connector.Connect();
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

    [SuppressMessage("ReSharper", "MethodHasAsyncOverload", Justification = "Internal method having both sync and async paths")]
    [SuppressMessage("ReSharper", "MethodHasAsyncOverloadWithCancellation", Justification = "Internal method having both sync and async paths")]
    private async Task LogInternalAsync(Message message, bool async, CancellationToken cancellationToken = default, bool retry = true)
    {
        if (_stream == Stream.Null)
        {
            if (async)
                await ConnectInternalAsync(async: true, cancellationToken);
            else
                ConnectInternalAsync(async: false, cancellationToken).GetAwaiter().GetResult();
        }

        try
        {
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
        catch (IOException) when (retry)
        {
            // TODO: could buffer the messages in a queue instead of just retrying once
            // For inspiration, see https://github.com/serilog-contrib/Serilog.Sinks.Network/blob/ce131dcea588d959f80e06965586dd5d35e6371a/Serilog.Sinks.Network/Sinks/TCP/TCPSocketWriter.cs#L31-L48
            // Also, detecting failure by catching exceptions is not enough! See https://stackoverflow.com/questions/31322716/tcpclient-networkstream-not-detecting-disconnection
            if (async)
                await _stream.DisposeAsync();
            else
                _stream.Dispose();

            _stream = Stream.Null;

            if (async)
                await LogInternalAsync(message, async, cancellationToken, retry: false);
            else
                LogInternalAsync(message, async, cancellationToken, retry: false).GetAwaiter().GetResult();
        }
    }

    public void Dispose()
    {
        CriticalSection(() =>
        {
            _stream.Dispose();
            _stream = new DisposedStream();
        });
        _semaphore.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        await CriticalSectionAsync(async _ =>
        {
            await _stream.DisposeAsync();
            _stream = new DisposedStream();
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