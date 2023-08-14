using System;
using System.Buffers;
using System.Buffers.Binary;

namespace NSLoggerSharp;

internal static class BufferWriterExtensions
{
    public static void WriteByte(this IBufferWriter<byte> writer, byte value)
    {
        writer.Write(stackalloc byte[sizeof(byte)] { value });
    }

    public static void WriteShort(this IBufferWriter<byte> writer, short value)
    {
        Span<byte> span = stackalloc byte[sizeof(short)];
        BinaryPrimitives.WriteInt16BigEndian(span, value);
        writer.Write(span);
    }

    public static void WriteInt(this IBufferWriter<byte> writer, int value)
    {
        Span<byte> span = stackalloc byte[sizeof(int)];
        BinaryPrimitives.WriteInt32BigEndian(span, value);
        writer.Write(span);
    }

    public static void WriteLong(this IBufferWriter<byte> writer, long value)
    {
        Span<byte> span = stackalloc byte[sizeof(long)];
        BinaryPrimitives.WriteInt64BigEndian(span, value);
        writer.Write(span);
    }
}