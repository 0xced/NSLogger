using System;
using System.IO;

namespace NSLoggerSharp;

internal class DisposedStream : Stream
{
    public override void Flush() => throw LoggerDisposedException();
    public override int Read(byte[] buffer, int offset, int count) => throw LoggerDisposedException();
    public override long Seek(long offset, SeekOrigin origin) => throw LoggerDisposedException();
    public override void SetLength(long value) => throw LoggerDisposedException();
    public override void Write(byte[] buffer, int offset, int count) => throw LoggerDisposedException();
    public override bool CanRead => throw LoggerDisposedException();
    public override bool CanSeek => throw LoggerDisposedException();
    public override bool CanWrite => throw LoggerDisposedException();
    public override long Length => throw LoggerDisposedException();
    public override long Position { get => throw LoggerDisposedException(); set => throw LoggerDisposedException(); }

    private static ObjectDisposedException LoggerDisposedException() => new(typeof(Logger).FullName);
}