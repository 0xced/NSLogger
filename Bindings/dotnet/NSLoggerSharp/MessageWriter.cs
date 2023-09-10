using System;
using System.Buffers;
using System.Text;

namespace NSLoggerSharp;

internal class MessageWriter
{
    private readonly ArrayBufferWriter<byte> _writer;
    private short _partCount;

    public MessageWriter(int initialCapacity = 1024)
    {
        _writer = new ArrayBufferWriter<byte>(initialCapacity);
    }

    public void WriteShort(PartKey key, short value)
    {
        _writer.WriteByte((byte)key);
        _writer.WriteByte((byte)PartType.Int16);
        _writer.WriteShort(value);

        _partCount++;
    }

    public void WriteInt(PartKey key, int value)
    {
        _writer.WriteByte((byte)key);
        _writer.WriteByte((byte)PartType.Int32);
        _writer.WriteInt(value);

        _partCount++;
    }

    public void WriteLong(PartKey key, long value)
    {
        _writer.WriteByte((byte)key);
        _writer.WriteByte((byte)PartType.Int64);
        _writer.WriteLong(value);

        _partCount++;
    }

    public void WriteString(PartKey key, string value)
    {
        WriteBytes(key, PartType.String, Encoding.UTF8.GetBytes(value));
    }

    public void WriteData(PartKey key, ReadOnlySpan<byte> data)
    {
        WriteBytes(key, PartType.Binary, data);
    }

    public void WriteImage(PartKey key, ReadOnlySpan<byte> data)
    {
        WriteBytes(key, PartType.Image, data);
    }

    private void WriteBytes(PartKey key, PartType type, ReadOnlySpan<byte> data)
    {
        _writer.WriteByte((byte)key);
        _writer.WriteByte((byte)type);
        _writer.WriteInt(data.Length);
        _writer.Write(data);

        _partCount++;
    }

    public ArrayBufferWriter<byte> FinalizeMessage()
    {
        const int totalSizeLength = sizeof(uint);
        const int partCountLength = sizeof(ushort);

        var messageWriter = new ArrayBufferWriter<byte>(totalSizeLength + partCountLength + _writer.WrittenSpan.Length);

        var totalSize = _writer.WrittenSpan.Length + partCountLength;
        messageWriter.WriteInt(totalSize);
        messageWriter.WriteShort(_partCount);
        messageWriter.Write(_writer.WrittenSpan);

        return messageWriter;
    }
}