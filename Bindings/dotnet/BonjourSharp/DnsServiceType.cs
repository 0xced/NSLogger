namespace BonjourSharp;

public enum DnsServiceType : ushort
{
    /// <summary>
    /// Host address.
    /// </summary>
    A = 1,

    /// <summary>
    /// Authoritative server.
    /// </summary>
    NS = 2,

    /// <summary>
    /// Mail destination.
    /// </summary>
    MD = 3,

    /// <summary>
    /// Mail forwarder.
    /// </summary>
    MF = 4,

    /// <summary>
    /// Canonical name.
    /// </summary>
    CNAME = 5,

    /// <summary>
    /// Start of authority zone.
    /// </summary>
    SOA = 6,

    /// <summary>
    /// Mailbox domain name.
    /// </summary>
    MB = 7,

    /// <summary>
    /// Mail group member.
    /// </summary>
    MG = 8,

    /// <summary>
    /// Mail rename name.
    /// </summary>
    MR = 9,

    /// <summary>
    /// Null resource record.
    /// </summary>
    NULL = 10,

    /// <summary>
    /// Well known service.
    /// </summary>
    WKS = 11,

    /// <summary>
    /// Domain name pointer.
    /// </summary>
    PTR = 12,

    /// <summary>
    /// Host information.
    /// </summary>
    HINFO = 13,

    /// <summary>
    /// Mailbox information.
    /// </summary>
    MINFO = 14,

    /// <summary>
    /// Mail routing information.
    /// </summary>
    MX = 15,

    /// <summary>
    /// One or more text strings.
    /// </summary>
    TXT = 16,

    /// <summary>
    /// Responsible person.
    /// </summary>
    RP = 17,

    /// <summary>
    /// AFS cell database.
    /// </summary>
    AFSDB = 18,

    /// <summary>
    /// X_25 calling address.
    /// </summary>
    X25 = 19,

    /// <summary>
    /// ISDN calling address.
    /// </summary>
    ISDN = 20,

    /// <summary>
    /// Router.
    /// </summary>
    RT = 21,

    /// <summary>
    /// NSAP address.
    /// </summary>
    NSAP = 22,

    /// <summary>
    /// Reverse NSAP lookup (deprecated).
    /// </summary>
    NSAP_PTR = 23,

    /// <summary>
    /// Security signature.
    /// </summary>
    SIG = 24,

    /// <summary>
    /// Security key.
    /// </summary>
    KEY = 25,

    /// <summary>
    /// X.400 mail mapping.
    /// </summary>
    PX = 26,

    /// <summary>
    /// Geographical position (withdrawn).
    /// </summary>
    GPOS = 27,

    /// <summary>
    /// IPv6 Address.
    /// </summary>
    AAAA = 28,

    /// <summary>
    /// Location Information.
    /// </summary>
    LOC = 29,

    /// <summary>
    /// Next domain (security).
    /// </summary>
    NXT = 30,

    /// <summary>
    /// Endpoint identifier.
    /// </summary>
    EID = 31,

    /// <summary>
    /// Nimrod Locator.
    /// </summary>
    NIMLOC = 32,

    /// <summary>
    /// Server Selection.
    /// </summary>
    SRV = 33,

    /// <summary>
    /// ATM Address
    /// </summary>
    ATMA = 34,

    /// <summary>
    /// Naming Authority PoinTeR
    /// </summary>
    NAPTR = 35,

    /// <summary>
    /// Key Exchange
    /// </summary>
    KX = 36,

    /// <summary>
    /// Certification record
    /// </summary>
    CERT = 37,

    /// <summary>
    /// IPv6 Address (deprecated)
    /// </summary>
    A6 = 38,

    /// <summary>
    /// Non-terminal DNAME (for IPv6)
    /// </summary>
    DNAME = 39,

    /// <summary>
    /// Kitchen sink (experimental)
    /// </summary>
    SINK = 40,

    /// <summary>
    /// EDNS0 option (meta-RR)
    /// </summary>
    OPT = 41,

    /// <summary>
    /// Transaction key
    /// </summary>
    TKEY = 249,

    /// <summary>
    /// Transaction signature.
    /// </summary>
    TSIG = 250,

    /// <summary>
    /// Incremental zone transfer.
    /// </summary>
    IXFR = 251,

    /// <summary>
    /// Transfer zone of authority.
    /// </summary>
    AXFR = 252,

    /// <summary>
    /// Transfer mailbox records.
    /// </summary>
    MAILB = 253,

    /// <summary>
    /// Transfer mail agent records.
    /// </summary>
    MAILA = 254,

    /// <summary>
    /// Wildcard match.
    /// </summary>
    ANY = 255,
}
