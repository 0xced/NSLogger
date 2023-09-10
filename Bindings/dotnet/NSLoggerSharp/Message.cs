using System;
using System.Buffers;
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

    internal static Message ClientInfo(LoggerOptions options)
        => new ClientInfoMessage(options);

    internal abstract void Serialize(MessageWriter writer);

    internal ArrayBufferWriter<byte> Serialize(int seq)
    {
        var writer = new MessageWriter();
        writer.WriteInt(PartKey.MessageSeq, seq);
        var milliseconds = Timestamp.ToUnixTimeMilliseconds();
        writer.WriteLong(PartKey.TimestampSeconds, milliseconds / 1000);
        writer.WriteLong(PartKey.TimestampMilliseconds, milliseconds % 1000);
        writer.WriteString(PartKey.ThreadId, ThreadName);

        Serialize(writer);

        if (Domain != null)
            writer.WriteString(PartKey.Tag, Domain);
        if (Level != 0)
            writer.WriteInt(PartKey.Level, Level);
        if (FileName != null)
            writer.WriteString(PartKey.FileName, FileName);
        if (LineNumber != 0)
            writer.WriteInt(PartKey.LineNumber, LineNumber);
        if (FunctionName != null)
            writer.WriteString(PartKey.FunctionName, FunctionName);

        return writer.FinalizeMessage();
    }

    private class TextMessage : Message
    {
        private readonly string _text;

        public TextMessage(string text, int level, string? domain = null, string? fileName = null, int lineNumber = 0, string? functionName = null, string? threadName = null, DateTimeOffset? timestamp = null)
            : base(level, domain, fileName, lineNumber, functionName, threadName, timestamp)
        {
            _text = text;
        }

        internal override void Serialize(MessageWriter writer)
        {
            writer.WriteInt(PartKey.MessageType, (int)MessageType.Log);
            writer.WriteString(PartKey.Message, _text);
        }
    }

    private class DataMessage : Message
    {
        private readonly byte[] _data;

        public DataMessage(byte[] data, int level, string? domain = null, string? fileName = null, int lineNumber = 0, string? functionName = null, string? threadName = null, DateTimeOffset? timestamp = null)
            : base(level, domain, fileName, lineNumber, functionName, threadName, timestamp)
        {
            _data = data;
        }

        internal override void Serialize(MessageWriter writer)
        {
            writer.WriteInt(PartKey.MessageType, (int)MessageType.Log);
            writer.WriteData(PartKey.Message, _data);
        }
    }

    private class ImageMessage : Message
    {
        private readonly byte[] _data;

        private readonly Size? _size;

        public ImageMessage(byte[] data, int level, Size? size = null, string? domain = null, string? fileName = null, int lineNumber = 0, string? functionName = null, string? threadName = null, DateTimeOffset? timestamp = null)
            : base(level, domain, fileName, lineNumber, functionName, threadName, timestamp)
        {
            _data = data;
            _size = size;
        }

        internal override void Serialize(MessageWriter writer)
        {
            writer.WriteInt(PartKey.MessageType, (int)MessageType.Log);
            if (_size.HasValue)
            {
                writer.WriteInt(PartKey.ImageWidth, _size.Value.Width);
                writer.WriteInt(PartKey.ImageHeight, _size.Value.Height);
            }
            writer.WriteImage(PartKey.Message, _data);
        }
    }

    private class MarkMessage : Message
    {
        private readonly string _text;

        public MarkMessage(string? text = null) : base(default, default, default, default, default, default, default)
        {
            _text = text ?? $"{DateTimeOffset.Now:G}";
        }

        internal override void Serialize(MessageWriter writer)
        {
            writer.WriteInt(PartKey.MessageType, (int)MessageType.Mark);
            writer.WriteString(PartKey.Message, _text);
        }
    }

    private class ClientInfoMessage : Message
    {
        private readonly LoggerOptions _options;

        public ClientInfoMessage(LoggerOptions options)
            : base(default, default, default, default, default, "", default)
        {
            _options = options;
        }

        internal override void Serialize(MessageWriter writer)
        {
            writer.WriteInt(PartKey.MessageType, (int)MessageType.ClientInfo);

            if (_options.ClientName != null)
                writer.WriteString(PartKey.ClientName, _options.ClientName);
            if (_options.ClientVersion != null)
                writer.WriteString(PartKey.ClientVersion, _options.ClientVersion);
            if (_options.ClientModel != null)
                writer.WriteString(PartKey.ClientModel, _options.ClientModel);
            if (_options.ClientUniqueIdentifier != null)
                writer.WriteString(PartKey.UniqueId, _options.ClientUniqueIdentifier);
            if (_options.OsName != null)
                writer.WriteString(PartKey.OsName, _options.OsName);
            if (_options.OsVersion != null)
                writer.WriteString(PartKey.OsVersion, _options.OsVersion);
        }
    }
}