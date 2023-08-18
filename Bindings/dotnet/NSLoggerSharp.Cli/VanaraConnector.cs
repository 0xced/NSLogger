using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Vanara.PInvoke;

namespace NSLoggerSharp.Cli;

public class VanaraConnector : LoggerConnector
{
    private readonly string? _serviceName;

    public VanaraConnector(string? serviceName) => _serviceName = serviceName;

    protected override async Task<IPEndPoint> GetEndPointAsync(CancellationToken cancellationToken = default)
    {
        var tcs = new TaskCompletionSource<IPEndPoint>();
        //var context = (IntPtr)GCHandle.Alloc(tcs);
        var request = new DnsApi.DNS_SERVICE_BROWSE_REQUEST
        {
            QueryName = "_nslogger-ssl._tcp.local",
            Version = 2,
            Callback = new DnsApi.DNS_SERVICE_BROWSE_REQUEST.DNS_SERVICE_BROWSE_REQUEST_CALLBACK()
            {
                pBrowseCallback = PBrowseCallback,
                pBrowseCallbackV2 = PBrowseCallbackV2,
            },
            //pQueryContext = context,
        };
        var status = DnsApi.DnsServiceBrowse(request, out var dnsCancel);
        return await tcs.Task;
    }

    private void PBrowseCallback(Win32Error status, IntPtr pquerycontext, IntPtr pdnsrecord)
    {
        throw new NotImplementedException();
    }

    private void PBrowseCallbackV2(IntPtr pquerycontext, ref DnsApi.DNS_QUERY_RESULT pqueryresults)
    {
        throw new NotImplementedException();
    }
}