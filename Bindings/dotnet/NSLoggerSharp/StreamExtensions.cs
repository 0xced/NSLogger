using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace NSLoggerSharp;

internal static class StreamExtensions
{
    public static void Write(this Stream stream, Message message, int seq)
    {
        WriteInternalAsync(stream, message, seq, async: false).GetAwaiter().GetResult();
    }

    public static async Task WriteAsync(this Stream stream, Message message, int seq, CancellationToken cancellationToken = default)
    {
        await WriteInternalAsync(stream, message, seq, async: true, cancellationToken);
    }

    [SuppressMessage("ReSharper", "MethodHasAsyncOverloadWithCancellation")]
    private static async Task WriteInternalAsync(Stream stream, Message message, int seq, bool async, CancellationToken cancellationToken = default)
    {
        var writer = new MessageWriter();
        writer.WriteInt(PartKey.MessageSeq, seq);
        var milliseconds = message.Timestamp.ToUnixTimeMilliseconds();
        writer.WriteLong(PartKey.TimestampSeconds, milliseconds / 1000);
        writer.WriteLong(PartKey.TimestampMilliseconds, milliseconds % 1000);
        writer.WriteString(PartKey.ThreadId, message.ThreadName);

        if (message is Message.ClientInfo clientInfo)
        {
            writer.WriteInt(PartKey.MessageType, (int)MessageType.ClientInfo);
            if (clientInfo.ClientName != null)
                writer.WriteString(PartKey.ClientName, clientInfo.ClientName);
            if (clientInfo.ClientVersion != null)
                writer.WriteString(PartKey.ClientVersion, clientInfo.ClientVersion);
            if (clientInfo.ClientModel != null)
                writer.WriteString(PartKey.ClientModel, clientInfo.ClientModel);
            if (clientInfo.ClientUniqueIdentifier != null)
                writer.WriteString(PartKey.UniqueId, clientInfo.ClientUniqueIdentifier);
            if (clientInfo.OsName != null)
                writer.WriteString(PartKey.OsName, clientInfo.OsName);
            if (clientInfo.OsVersion != null)
                writer.WriteString(PartKey.OsVersion, clientInfo.OsVersion);
        }
        else if (message is Message.MarkMessage mark)
        {
            writer.WriteInt(PartKey.MessageType, (int)MessageType.Mark);
            writer.WriteString(PartKey.Message, mark.Payload);
        }
        else
        {
            writer.WriteInt(PartKey.MessageType, (int)MessageType.Log);

            if (message.Domain != null)
                writer.WriteString(PartKey.Tag, message.Domain);
            if (message.Level != 0)
                writer.WriteInt(PartKey.Level, message.Level);
            if (message.FileName != null)
                writer.WriteString(PartKey.FileName, message.FileName);
            if (message.LineNumber != 0)
                writer.WriteInt(PartKey.LineNumber, message.LineNumber);
            if (message.FunctionName != null)
                writer.WriteString(PartKey.FunctionName, message.FunctionName);

            if (message is Message.TextMessage text)
            {
                writer.WriteString(PartKey.Message, text.Payload);
            }
            else if (message is Message.DataMessage data)
            {
                writer.WriteData(PartKey.Message, data.Payload);
            }
            else if (message is Message.ImageMessage image)
            {
                if (image.Size.HasValue)
                {
                    var imageSize = image.Size.Value;
                    writer.WriteInt(PartKey.ImageWidth, imageSize.Width);
                    writer.WriteInt(PartKey.ImageHeight, imageSize.Height);
                }
                writer.WriteImage(PartKey.Message, image.Payload);
            }
            else
            {
                throw new NotImplementedException($"Message of type {message.GetType().FullName} is not implemented.");
            }
        }

        if (async)
            await writer.FinalizeAsync(stream, cancellationToken);
        else
            writer.Finalize(stream);
    }
}