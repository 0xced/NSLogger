using System;
using System.Collections.Generic;
using System.Linq;
using Zeroconf;

namespace NSLoggerSharp.Zeroconf;

public class BonjourServiceNotFoundException : Exception
{
    public BonjourServiceNotFoundException()
        : base("No NSLogger Bonjour service was found. " +
               "Is the NSLogger app running and on the same network?")
    {
    }

    public BonjourServiceNotFoundException(IZeroconfHost host, string serviceName)
        : base($"The NSLogger Bonjour service named \"{serviceName}\" was not found on {host.Id}. " +
               "Does it match the service name configured in the NSLogger preferences?")
    {
    }

    public BonjourServiceNotFoundException(IZeroconfHost host)
        : base(GetMessage(host))
    {
    }

    private static string GetMessage(IZeroconfHost host)
    {
        var filteredClientNames = host.Services.Values.Where(e => e.FilterClients()).Select(e => e.ServiceName.Replace($".{e.Name}", "")).ToList();
        var resolutionHint = filteredClientNames.Count switch
        {
            0 => $": loggerConfiguration.BonjourServiceName = \"{host.DisplayName}\"",
            1 => $": loggerConfiguration.BonjourServiceName = \"{filteredClientNames[0]}\"",
            _ => $".{Environment.NewLine}{string.Join(Environment.NewLine, filteredClientNames.Select(e => $"  \u2022 loggerConfiguration.BonjourServiceName = \"{e}\""))}"
        };
        return $"NSLogger is running on {host.Id} but the Bonjour service name must be specified explicitly in the configuration{resolutionHint}";
    }

    public BonjourServiceNotFoundException(IZeroconfHost host, IEnumerable<IService> unfilteredServices)
        : base(GetMessage(host, unfilteredServices))
    {
    }

    private static string GetMessage(IZeroconfHost host, IEnumerable<IService> unfilteredServices)
    {
        var configurations = string.Join(Environment.NewLine, unfilteredServices.Select(e => e.ServiceName.Replace($".{e.Name}", "")).Select(e => $"  \u2022 loggerConfiguration.BonjourServiceName = \"{e}\""));
        return $"Multiple NSLogger instances are running on {host.Id}. " +
               $"Either close all except one instance or specify explicitly the name of the service in the configuration.{Environment.NewLine}{configurations}";
    }
}