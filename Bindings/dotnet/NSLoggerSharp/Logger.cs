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
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly LoggerOptions _options;
    private Stream _stream = Stream.Null;
    private TcpClient? _client;
    private int _seq;

    public Logger() : this(new LoggerOptions())
    {
    }

    public Logger(LoggerOptions options)
    {
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
        _client = new TcpClient();

        if (async)
            await _client.ConnectAsync(_options.ViewerHost, cancellationToken);
        else
            _client.Connect(_options.ViewerHost);

        var sslStream = new SslStream(_client.GetStream(), leaveInnerStreamOpen: false, _options.ValidateCertificate, userCertificateSelectionCallback: null);

        _stream = sslStream;

        var authenticationOptions = new SslClientAuthenticationOptions { TargetHost = _options.ViewerHost.ToString() };
        if (async)
        {
            await sslStream.AuthenticateAsClientAsync(authenticationOptions, cancellationToken);
            await LogInternalAsync(new Message.ClientInfo(_options), async: true, cancellationToken);
        }
        else
        {
            sslStream.AuthenticateAsClient(authenticationOptions);
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
            _client?.Dispose();
            _client = null;
        });
        _semaphore.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        await CriticalSectionAsync(async _ =>
        {
            await _stream.DisposeAsync();
            _stream = new DisposedStream();
            _client?.Dispose();
            _client = null;
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