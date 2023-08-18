using System;
using System.Threading;
using System.Threading.Tasks;
using BonjourSharp;

var cancelKeyPressSource = new CancellationTokenSource();

Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = !cancelKeyPressSource.IsCancellationRequested;
    cancelKeyPressSource.Cancel();
};

try
{
    var timeoutSource = new CancellationTokenSource();
    timeoutSource.CancelAfter(TimeSpan.FromSeconds(500));
    var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(timeoutSource.Token, cancelKeyPressSource.Token);
    await RunAsync(linkedSource.Token);
    Console.WriteLine("✅ Done");
}
catch (OperationCanceledException)
{
    Console.WriteLine("🛑 Cancelled");
}

async Task RunAsync(CancellationToken cancellationToken)
{
    await foreach (var service in DnsServiceBrowser.BrowseAsync("_companion-link._tcp", cancellationToken: cancellationToken))
    {
        Console.WriteLine(service);
    }
}
