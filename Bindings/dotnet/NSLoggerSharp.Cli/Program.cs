using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using NSLoggerSharp;
using NSLoggerSharp.Cli;
using NSLoggerSharp.Zeroconf;
using Spectre.Console;

var cancellationTokenSource = new CancellationTokenSource();

Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = !cancellationTokenSource.IsCancellationRequested;
    cancellationTokenSource.Cancel();
};

try
{
    await RunAsync(args, cancellationTokenSource.Token);
    AnsiConsole.WriteLine("✔️ Done");
}
catch (OperationCanceledException)
{
    AnsiConsole.WriteLine("🛑 Cancelled");
}
catch (Exception exception)
{
    AnsiConsole.WriteLine("❌ Error");
    AnsiConsole.WriteException(exception, ExceptionFormats.ShortenPaths);
}

static async Task RunAsync(IReadOnlyList<string> args, CancellationToken cancellationToken)
{
    if (!File.Exists("sample.png"))
    {
        await using var fileStream = new FileStream("sample.png", FileMode.CreateNew, FileAccess.Write);
        using var client = new HttpClient();
        await using var httpStream = await client.GetStreamAsync("https://upload.wikimedia.org/wikipedia/commons/thumb/2/21/Matterhorn_sunset_2016_%28Unsplash%29.jpg/440px-Matterhorn_sunset_2016_%28Unsplash%29.jpg", cancellationToken);
        await httpStream.CopyToAsync(fileStream, cancellationToken);
    }

    var imageData = await File.ReadAllBytesAsync("sample.png", cancellationToken);

    var options = await ParseLoggerOptionsAsync(args, cancellationToken);
    await using var logger = new Logger(options);
    await logger.ConnectAsync(cancellationToken);

    logger.Log(new Message.Text("👋 Hello, world!", level: 0));
    logger.Log(new Message.Text($"First line{Environment.NewLine}Second line", level: 1));
    await Task.Yield();
    logger.Log(new Message.Text("With a domain", level: 2, domain: "Domain 📗 with emoji"));
    logger.Log(new Message.Mark());
    var frame = new StackFrame(skipFrames: 0, needFileInfo: true);
    logger.Log(new Message.Text("With file and function", level: 3, fileName: frame.GetFileName(), lineNumber: frame.GetFileLineNumber() + 1, functionName: frame.GetMethod()?.Name));
    logger.Log(new Message.Text("With a thread name", level: 4, threadName: "Custom 🧵 name"));
    logger.Log(new Message.Data(imageData, level: 5));
    logger.Log(new Message.Image(imageData, level: 6));
}

static async Task<LoggerOptions> ParseLoggerOptionsAsync(IReadOnlyList<string> args, CancellationToken cancellationToken)
{
    if (args.Count > 0)
    {
        if (IPEndPoint.TryParse(args[0], out var viewerHost))
        {
            return new LoggerOptions { ViewerHost = viewerHost };
        }

        IBonjourResolver resolver = args.Count > 1 && args[1] == "Zeroconf" ? new BonjourResolver() : new ArkaneResolver();
        return new LoggerOptions { ViewerHost = await resolver.ResolveBonjourServiceAsync(args[0], cancellationToken) };
    }

    return new LoggerOptions();
}