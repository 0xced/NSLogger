using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using NSLoggerSharp.Zeroconf;
using Spectre.Console;
using Spectre.Console.Cli;

namespace NSLoggerSharp.Cli;

public class DefaultCommand : AsyncCommand<DefaultCommand.Settings>
{
    private readonly IAnsiConsole _console;
    private readonly CancellationToken _cancellationToken;

    public enum BonjourImplementation
    {
        Arkane,
        ArkaneBrowse,
        ArkaneEnumerate,
        BonjourSharp,
        Mittosoft,
        Vanara,
        Zeroconf,
    }

    public class Settings : CommandSettings
    {
        [Description("Either an IPEndpoint (e.g [b]127.0.0.1:1234[/]) or the NSLogger Bonjour service name to search for. " +
                     "Uses the first NSLogger instance that accepts any client if not specified.")]
        [CommandOption("--viewer")]
        public string? Viewer { get; init; }

        [Description("The Bonjour implementation to use.")]
        [CommandOption("--implementation")]
        [DefaultValue(BonjourImplementation.Zeroconf)]
        public BonjourImplementation Implementation { get; init; }

        [Description("The URL of the image to send.")]
        [CommandOption("--imageUrl")]
        [DefaultValue("https://upload.wikimedia.org/wikipedia/commons/thumb/2/21/Matterhorn_sunset_2016_%28Unsplash%29.jpg/440px-Matterhorn_sunset_2016_%28Unsplash%29.jpg")]
        public Uri ImageUrl { get; init; } = default!;
    }

    public DefaultCommand(IAnsiConsole console, CancellationToken cancellationToken)
    {
        _console = console;
        _cancellationToken = cancellationToken;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        var fileName = Path.GetFileName(settings.ImageUrl.LocalPath);
        if (!File.Exists(fileName))
        {
            await using var fileStream = new FileStream(fileName, FileMode.CreateNew, FileAccess.Write);
            using var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("NSLoggerSample", "1.0"));
            await using var httpStream = await client.GetStreamAsync(settings.ImageUrl, _cancellationToken);
            await httpStream.CopyToAsync(fileStream, _cancellationToken);
        }

        var imageData = await File.ReadAllBytesAsync(fileName, _cancellationToken);

        var connector = GetLoggerConnector(settings.Viewer, settings.Implementation);

        _console.Write("Connecting to ");
        if (settings.Viewer == null)
            _console.Write("first NSLogger found", Color.Aqua);
        else
            _console.Write(settings.Viewer, Color.Lime);
        _console.MarkupLineInterpolated($" with [aqua]{connector.GetType().Name}[/]");

        await using var logger = new Logger(connector);
        await logger.ConnectAsync(_cancellationToken);

        await logger.LogAsync(Message.Text("👋 Hello, world!", level: 0), _cancellationToken);
        await logger.LogAsync(Message.Text($"First line{Environment.NewLine}Second line", level: 1), _cancellationToken);
        await Task.Delay(TimeSpan.FromSeconds(1), _cancellationToken);
        await logger.LogAsync(Message.Text("With a domain", level: 2, domain: "Custom 📗 domain"), _cancellationToken);
        await logger.LogAsync(Message.Mark(), _cancellationToken);
        var frame = new StackFrame(skipFrames: 0, needFileInfo: true);
        await logger.LogAsync(Message.Text("With file and function", level: 3, fileName: frame.GetFileName(), lineNumber: frame.GetFileLineNumber() + 1, functionName: frame.GetMethod()?.Name), _cancellationToken);
        await logger.LogAsync(Message.Text("With a thread name", level: 4, threadName: "Custom 🧵 name"), _cancellationToken);
        await logger.LogAsync(Message.Data(imageData, level: 5), _cancellationToken);
        await logger.LogAsync(Message.Image(imageData, level: 6), _cancellationToken);

        return 0;
    }

    private static ILoggerConnector GetLoggerConnector(string? viewer, BonjourImplementation bonjourImplementation)
    {
        if (viewer != null && IPEndPoint.TryParse(viewer, out var viewerHost))
        {
            return new LoggerConnector(viewerHost);
        }

        return bonjourImplementation switch
        {
            BonjourImplementation.Arkane => new ArkaneConnector(viewer),
            BonjourImplementation.ArkaneBrowse => new ArkaneBrowseConnector(viewer),
            BonjourImplementation.ArkaneEnumerate => new ArkaneEnumerateConnector(viewer),
            BonjourImplementation.BonjourSharp => new BonjourSharpConnector(viewer),
            BonjourImplementation.Mittosoft => new MittosoftConnector(viewer),
            BonjourImplementation.Vanara => new VanaraConnector(viewer),
            BonjourImplementation.Zeroconf => new ZeroconfConnector(viewer),
            _ => throw new ArgumentOutOfRangeException(nameof(bonjourImplementation), bonjourImplementation, null)
        };
    }
}