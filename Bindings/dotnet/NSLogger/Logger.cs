using static NSLogger.NativeMethods.NSLogger;

namespace NSLogger;

[SupportedOSPlatform("macos")]
public sealed class Logger : IDisposable
{
    private readonly LoggerConfiguration _configuration;
    private IntPtr _pointer = IntPtr.Zero;
    private readonly object _loggerLock = new();

    public static implicit operator IntPtr(Logger logger) => logger._pointer;

    public Logger() : this(new LoggerConfiguration())
    {
    }

    public Logger(LoggerConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    public void LogMessage(string message, int level, string? domain = null, string? fileName = null, int lineNumber = 0, string? functionName = null, string? threadName = null, DateTimeOffset? timestamp = null)
    {
        ArgumentNullException.ThrowIfNull(message);

        using var domainRef = new CFTypeRef(domain);
        using var messageRef = new CFTypeRef(message);

        lock (_loggerLock)
        {
            EnsureInitialized();
            using (AdjustLoggerParameters(this, threadName, timestamp))
            {
                LogMessageRawToF(_pointer, fileName, lineNumber, functionName, domainRef, level, messageRef);
            }
        }
    }

    public void LogData(ReadOnlySpan<byte> data, int level, string? domain = null, string? fileName = null, int lineNumber = 0, string? functionName = null, string? threadName = null, DateTimeOffset? timestamp = null)
    {
        using var domainRef = new CFTypeRef(domain);
        using var dataRef = new CFTypeRef(data);

        lock (_loggerLock)
        {
            EnsureInitialized();
            using (AdjustLoggerParameters(this, threadName, timestamp))
            {
                LogDataToF(_pointer, fileName, lineNumber, functionName, domainRef, level, dataRef);
            }
        }
    }

    public void LogImage(ReadOnlySpan<byte> imageData, int level, string? domain = null, string? fileName = null, int lineNumber = 0, string? functionName = null, string? threadName = null, DateTimeOffset? timestamp = null)
    {
        using var domainRef = new CFTypeRef(domain);
        using var imageDataRef = new CFTypeRef(imageData);

        lock (_loggerLock)
        {
            EnsureInitialized();
            using (AdjustLoggerParameters(this, threadName, timestamp))
            {
                LogImageDataToF(_pointer, fileName, lineNumber, functionName, domainRef, level, width: 0, height: 0, imageDataRef);
            }
        }
    }

    private void EnsureInitialized()
    {
        if (_pointer == IntPtr.Zero)
        {
            _pointer = LoggerInit();
            LoggerSetOptions(_pointer, (uint)_configuration.Options);
            using var clientName = new CFTypeRef(_configuration.ClientName);
            using var clientVersion = new CFTypeRef(_configuration.ClientVersion);
            LoggerSetClient(_pointer, clientName, clientVersion);
            if (_configuration.ViewerHost != null)
            {
                using var host = new CFTypeRef(_configuration.ViewerHost.Address.ToString());
                LoggerSetViewerHost(_pointer, host, (uint)_configuration.ViewerHost.Port);
            }
            else
            {
                using var bonjourServiceNameRef = new CFTypeRef(_configuration.BonjourServiceName);
                LoggerSetupBonjour(_pointer, IntPtr.Zero, bonjourServiceNameRef);
            }
        }
    }

    public void Dispose()
    {
        LoggerFlush(_pointer, waitForConnection: _configuration.WaitOnDispose);
        LoggerStop(_pointer);
    }

    internal static IDisposable AdjustLoggerParameters(Logger logger, string? threadName, DateTimeOffset? date) => new Adjuster(logger, threadName, date);

    private class Adjuster : IDisposable
    {
        private readonly IntPtr _oldThreadName = IntPtr.Zero;

        public Adjuster(Logger logger, string? newThreadName, DateTimeOffset? date)
        {
            if (newThreadName != null)
            {
                _oldThreadName = NSLoggerGetCurrentThreadName();
                using var threadNameRef = new CFTypeRef(newThreadName);
                NSLoggerSetCurrentThreadName(threadNameRef);
            }

            if (date.HasValue)
            {
                LoggerSetTimestamp(logger, new timeval(date.Value));
            }
        }

        public void Dispose()
        {
            if (_oldThreadName != IntPtr.Zero)
            {
                NSLoggerSetCurrentThreadName(_oldThreadName);
            }
        }
    }
}