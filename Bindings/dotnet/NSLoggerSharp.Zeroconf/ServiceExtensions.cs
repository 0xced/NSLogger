using System.Linq;
using Zeroconf;

namespace NSLoggerSharp.Zeroconf;

internal static class ServiceExtensions
{
    public static bool FilterClients(this IService service)
    {
        return service.Properties.Any(e => e.TryGetValue("filterClients", out var filterClients) && filterClients == "1");
    }
}