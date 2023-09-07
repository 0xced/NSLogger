using System;
using System.Drawing;

namespace NSLoggerSharp;

public abstract class Message
{
    internal int Level { get; }
    internal string? Domain { get; }
    internal string? FileName { get; }
    internal int LineNumber { get; }
    internal string? FunctionName { get; }
    internal string ThreadName { get; }
    internal DateTimeOffset Timestamp { get; }

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

    public static Message Text(string text, int level, string? domain = null, string? fileName = null, int lineNumber = 0, string? functionName = null, string? threadName = null, DateTimeOffset? timestamp = null)
        => new TextMessage(text, level, domain, fileName, lineNumber, functionName, threadName, timestamp);

    public static Message Data(byte[] data, int level, string? domain = null, string? fileName = null, int lineNumber = 0, string? functionName = null, string? threadName = null, DateTimeOffset? timestamp = null)
        => new DataMessage(data, level, domain, fileName, lineNumber, functionName, threadName, timestamp);

    public static Message Image(byte[] data, int level, Size? size = null, string? domain = null, string? fileName = null, int lineNumber = 0, string? functionName = null, string? threadName = null, DateTimeOffset? timestamp = null)
        => new ImageMessage(data, level, size, domain, fileName, lineNumber, functionName, threadName, timestamp);

    public static Message Mark(string? text = null)
        => new MarkMessage(text);

    internal class TextMessage : Message
    {
        public TextMessage(string text, int level, string? domain = null, string? fileName = null, int lineNumber = 0, string? functionName = null, string? threadName = null, DateTimeOffset? timestamp = null)
            : base(level, domain, fileName, lineNumber, functionName, threadName, timestamp)
        {
            Payload = text;
        }

        public string Payload { get; }
    }

    internal class DataMessage : Message
    {
        public DataMessage(byte[] data, int level, string? domain = null, string? fileName = null, int lineNumber = 0, string? functionName = null, string? threadName = null, DateTimeOffset? timestamp = null)
            : base(level, domain, fileName, lineNumber, functionName, threadName, timestamp)
        {
            Payload = data;
        }

        public byte[] Payload { get; }
    }

    internal class ImageMessage : Message
    {
        public ImageMessage(byte[] data, int level, Size? size = null, string? domain = null, string? fileName = null, int lineNumber = 0, string? functionName = null, string? threadName = null, DateTimeOffset? timestamp = null)
            : base(level, domain, fileName, lineNumber, functionName, threadName, timestamp)
        {
            Payload = data;
            Size = size;
        }

        public byte[] Payload { get; }

        public Size? Size { get; }
    }

    internal class MarkMessage : Message
    {
        public MarkMessage(string? text = null) : base(default, default, default, default, default, default, default)
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