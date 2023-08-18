using System;
using System.Threading;
using NSLoggerSharp.Cli;
using Spectre.Console;
using Spectre.Console.Cli;

var cancellationTokenSource = new CancellationTokenSource();

Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = !cancellationTokenSource.IsCancellationRequested;
    cancellationTokenSource.Cancel();
};

try
{
    var app = new CommandApp<DefaultCommand>();
    app.Configure(config =>
    {
        config.PropagateExceptions();
        config.Settings.Registrar.RegisterInstance(cancellationTokenSource.Token);
    });
    await app.RunAsync(args);
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