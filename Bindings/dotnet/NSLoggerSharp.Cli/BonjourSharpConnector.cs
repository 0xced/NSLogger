using System;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using BonjourSharp;

namespace NSLoggerSharp.Cli;

public class BonjourSharpConnector : LoggerConnector
{
    private readonly string? _serviceName;

    public BonjourSharpConnector(string? serviceName) => _serviceName = serviceName;

    protected override async Task<IPEndPoint> GetEndPointAsync(CancellationToken cancellationToken = default)
    {
        var taskCompletionSource = new TaskCompletionSource<IPEndPoint>();
        using var source = new CancellationTokenSource();
        using var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(source.Token, cancellationToken);
        var context = new Context(_serviceName, taskCompletionSource, linkedSource);
        var error = DnsServiceDiscovery.DnsServiceBrowse(out var sdRef, flags: DnsServiceFlags.None, interfaceIndex: 0, "_nslogger-ssl._tcp", domain: null, callback: OnServiceBrowsed, context);
        //await sdRef.ProcessAsync(linkedSource.Token);
        var result = await taskCompletionSource.Task;
        sdRef.Dispose();
        return result;
    }

    private static void OnServiceBrowsed(DnsServiceRef sdRef, DnsServiceFlags flags, uint interfaceIndex, DnsServiceError errorCode, IntPtr serviceName, string regType, string replyDomain, IntPtr context)
    {
        var name = Marshal.PtrToStringUTF8(serviceName) ?? throw new ArgumentException("The service name can not be null.", nameof(serviceName));
        var ctx = GCHandle.FromIntPtr(context).Target as Context ?? throw new ArgumentException($"Must hold a {nameof(Context)} instance.", nameof(context));
        if (ctx.ServiceName == null || ctx.ServiceName == name)
        {
            var error = DnsServiceDiscovery.DnsServiceResolve(out var resolveRef, DnsServiceFlags.None, interfaceIndex, name, regType, replyDomain, OnServiceResolved, context);
            while (DnsServiceDiscovery.DnsServiceProcessResult(resolveRef) == DnsServiceError.NoError && !ctx.CancellationTokenSource.IsCancellationRequested)
            {
            }
            //context.CancellationTokenSource.Cancel();
        }
    }

    private static void OnServiceResolved(DnsServiceRef sdref, DnsServiceFlags flags, uint interfaceindex, DnsServiceError errorcode, IntPtr fullname, string hosttarget, ushort port, ushort txtlen, IntPtr txtrecord, IntPtr context)
    {
        var name = Marshal.PtrToStringUTF8(fullname) ?? throw new ArgumentException("The service name can not be null.", nameof(fullname));
        var ctx = GCHandle.FromIntPtr(context).Target as Context ?? throw new ArgumentException($"Must hold a {nameof(Context)} instance.", nameof(context));
        ctx.CancellationTokenSource.Cancel();
    }

    private class Context
    {
        private readonly GCHandle _handle;

        public Context(string? serviceName, TaskCompletionSource<IPEndPoint> taskCompletionSource, CancellationTokenSource cancellationTokenSource)
        {
            ServiceName = serviceName;
            TaskCompletionSource = taskCompletionSource;
            CancellationTokenSource = cancellationTokenSource;
            _handle = GCHandle.Alloc(this);
        }

        public static implicit operator IntPtr(Context context) => GCHandle.ToIntPtr(context._handle);

        public string? ServiceName { get; }
        public TaskCompletionSource<IPEndPoint> TaskCompletionSource { get; }
        public CancellationTokenSource CancellationTokenSource { get; }
    }
}