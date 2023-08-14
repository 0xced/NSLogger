using System;
using System.Drawing;

namespace NSLoggerSharp;

public abstract class Message
{
    public int Level { get; }
    public string? Domain { get; }
    public string? FileName { get; }
    public int LineNumber { get; }
    public string? FunctionName { get; }
    public string ThreadName { get; }
    public DateTimeOffset Timestamp { get; }

    private Message(int level, string? domain, string? fileName, int lineNumber, string? functionName, string? threadName, DateTimeOffset? timestamp)
    {
        Level = level;
        Domain = domain;
        FileName = fileName;
        LineNumber = lineNumber;
        FunctionName = functionName;
        ThreadName = threadName ?? $"Thread {Environment.CurrentManagedThreadId}";
        Timestamp = timestamp ?? DateTimeOffset.Now;
    }

    public class Text : Message
    {
        public Text(string text, int level, string? domain = null, string? fileName = null, int lineNumber = 0, string? functionName = null, string? threadName = null, DateTimeOffset? timestamp = null)
            : base(level, domain, fileName, lineNumber, functionName, threadName, timestamp)
        {
            Payload = text;
        }

        public string Payload { get; }
    }

    public class Data : Message
    {
        public Data(byte[] data, int level, string? domain = null, string? fileName = null, int lineNumber = 0, string? functionName = null, string? threadName = null, DateTimeOffset? timestamp = null)
            : base(level, domain, fileName, lineNumber, functionName, threadName, timestamp)
        {
            Payload = data;
        }

        public byte[] Payload { get; }
    }

    public class Image : Message
    {
        public Image(byte[] data, int level, Size? size = null, string? domain = null, string? fileName = null, int lineNumber = 0, string? functionName = null, string? threadName = null, DateTimeOffset? timestamp = null)
            : base(level, domain, fileName, lineNumber, functionName, threadName, timestamp)
        {
            Payload = data;
            Size = size;
        }

        public byte[] Payload { get; }

        public Size? Size { get; }
    }

#if MESSAGE_BLOCK_SUPPORT
    public class StartBlock : Message
    {
        public StartBlock() : base(default, default, default, default, default, default, default)
        {
        }
    }

    public class EndBlock : Message
    {
        public EndBlock() : base(default, default, default, default, default, default, default)
        {
        }
    }
#endif

    public class Mark : Message
    {
        public Mark(string? text = null) : base(default, default, default, default, default, default, default)
        {
            Payload = text ?? $"{DateTimeOffset.Now:G}";
        }

        public string Payload { get; }
    }

    internal class ClientInfo : Message
    {
        private readonly LoggerOptions _options;

        public string? ClientName => _options.ClientName;
        public string? ClientVersion => _options.ClientVersion;
        public string? ClientModel => _options.ClientModel;
        public string? OsName => _options.OsName;
        public string? OsVersion => _options.OsVersion;

        public ClientInfo(LoggerOptions options)
            : base(default, default, default, default, default, default, default)
        {
            _options = options;
        }
    }
}