namespace BonjourSharp ;

public enum DnsServiceError
{
    NoError                   = 0,
    Unknown                   = -65537,
    NoSuchName                = -65538,
    NoMemory                  = -65539,
    BadParam                  = -65540,
    BadReference              = -65541,
    BadState                  = -65542,
    BadFlags                  = -65543,
    Unsupported               = -65544,
    NotInitialized            = -65545,
    AlreadyRegistered         = -65547,
    NameConflict              = -65548,
    Invalid                   = -65549,
    Firewall                  = -65550,
    /// <summary>Client library incompatible with daemon.</summary>
    Incompatible              = -65551,
    BadInterfaceIndex         = -65552,
    Refused                   = -65553,
    NoSuchRecord              = -65554,
    NoAuth                    = -65555,
    NoSuchKey                 = -65556,
    NatTraversal              = -65557,
    DoubleNat                 = -65558,
    /// <summary>Codes up to here existed in Tiger.</summary>
    BadTime                   = -65559,
    BadSig                    = -65560,
    BadKey                    = -65561,
    Transient                 = -65562,
    /// <summary>Background daemon not running.</summary>
    ServiceNotRunning         = -65563,
    /// <summary>NAT doesn't support NAT-PMP or UPnP.</summary>
    NatPortMappingUnsupported = -65564,
    /// <summary>NAT supports NAT-PMP or UPnP but it's disabled by the administrator.</summary>
    NatPortMappingDisabled    = -65565,
    /// <summary>No router currently configured (probably no network connectivity).</summary>
    NoRouter                  = -65566,
    PollingMode               = -65567,
    Timeout                   = -65568,
}
