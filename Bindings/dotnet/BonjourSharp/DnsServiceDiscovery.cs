using System;
using System.Runtime.InteropServices;

namespace BonjourSharp;

public class DnsServiceDiscoveryException : Exception
{
    public DnsServiceError Error { get; }

    private DnsServiceDiscoveryException(DnsServiceError error, string methodName) : base($"{methodName} failed with error \"{error}\"")
    {
        Error = error;
    }

    public static void ThrowIfError(DnsServiceError error, string methodName)
    {
        if (error != DnsServiceError.NoError)
        {
            throw new DnsServiceDiscoveryException(error, methodName);
        }
    }
}

public static class DnsServiceDiscovery
{
    /// <summary>
    /// Copy the "Constants for specifying an interface index" doc here
    /// </summary>
    public const uint DnsServiceInterfaceIndexAny = 0;
    public const uint DnsServiceInterfaceIndexLocalOnly = unchecked((uint)-1);
    public const uint DnsServiceInterfaceIndexUnicast = unchecked((uint)-2);
    public const uint DnsServiceInterfaceIndexP2P = unchecked((uint)-3);

    //private const string DllName = "dnssd.dll";
    private const string DllName = "system";

    [DllImport(DllName, EntryPoint = "DNSServiceEnumerateDomains")]
    public static extern void DnsServiceEnumerateDomains(DnsServiceRef sdRef, DnsServiceFlags flags, uint interfaceIndex, IntPtr callback, IntPtr context);

    /* DNSServiceQueryRecord
 *
 * Query for an arbitrary DNS record.
 *
 * DNSServiceQueryRecordReply() Callback Parameters:
 *
 * sdRef:           The DNSServiceRef initialized by DNSServiceQueryRecord().
 *
 * flags:           Possible values are kDNSServiceFlagsMoreComing and
 *                  kDNSServiceFlagsAdd. The Add flag is NOT set for PTR records
 *                  with a ttl of 0, i.e. "Remove" events.
 *
 * interfaceIndex:  The interface on which the query was resolved (the index for a given
 *                  interface is determined via the if_nametoindex() family of calls).
 *                  See "Constants for specifying an interface index" for more details.
 *
 * errorCode:       Will be kDNSServiceErr_NoError on success, otherwise will
 *                  indicate the failure that occurred. Other parameters are undefined if
 *                  errorCode is nonzero.
 *
 * fullname:        The resource record's full domain name.
 *
 * rrtype:          The resource record's type (e.g. kDNSServiceType_PTR, kDNSServiceType_SRV, etc)
 *
 * rrclass:         The class of the resource record (usually kDNSServiceClass_IN).
 *
 * rdlen:           The length, in bytes, of the resource record rdata.
 *
 * rdata:           The raw rdata of the resource record.
 *
 * ttl:             If the client wishes to cache the result for performance reasons,
 *                  the TTL indicates how long the client may legitimately hold onto
 *                  this result, in seconds. After the TTL expires, the client should
 *                  consider the result no longer valid, and if it requires this data
 *                  again, it should be re-fetched with a new query. Of course, this
 *                  only applies to clients that cancel the asynchronous operation when
 *                  they get a result. Clients that leave the asynchronous operation
 *                  running can safely assume that the data remains valid until they
 *                  get another callback telling them otherwise. The ttl value is not
 *                  updated when the daemon answers from the cache, hence relying on
 *                  the accuracy of the ttl value is not recommended.
 *
 * context:         The context pointer that was passed to the callout.
 *
 */

    public unsafe delegate void DnsServiceQueryRecordReply(DnsServiceRef sdRef, DnsServiceFlags flags, uint interfaceIndex, DnsServiceError errorCode, string fullName, DnsServiceType rrType, DnsServiceClass rrClass, ushort rdLen, byte* rData, uint ttl, IntPtr context);

    /// <param name="sdRef">
    /// A pointer to an uninitialized <see cref="DnsServiceRef"/> (or, if the <see cref="DnsServiceFlags.ShareConnection"/> flag is used,
    /// a copy of the shared connection reference that is to be used). If the call succeeds then it initializes (or updates) the
    /// <see cref="DnsServiceRef"/>, returns <see cref="DnsServiceError.NoError"/>, and the query operation will remain active indefinitely
    /// until the client terminates it by passing this <see cref="DnsServiceRef"/> to <see cref="DnsServiceRefDeallocate"/>
    /// (or by closing the underlying shared connection, if used).
    /// </param>
    /// <param name="flags">
    /// Possible values are:
    /// <list type="bullet">
    ///   <item><see cref="DnsServiceFlags.ShareConnection"/> to use a shared connection.</item>
    ///   <item><see cref="DnsServiceFlags.ForceMulticast"/> or <see cref="DnsServiceFlags.LongLivedQuery"/>.</item>
    /// </list>
    /// Pass <see cref="DnsServiceFlags.LongLivedQuery"/> to create a "long-lived" unicast query to a unicast DNS server that implements
    /// the protocol. This flag has no effect on link-local multicast queries.
    /// </param>
    /// <param name="interfaceIndex">
    /// If non-zero, specifies the interface on which to issue the query (the index for a given interface is determined via the if_nametoindex() family of calls.)
    /// Passing <see cref="DnsServiceInterfaceIndexAny"/> causes the name to be queried for on all interfaces.
    /// </param>
    /// <param name="fullName">The full domain name of the resource record to be queried for.</param>
    /// <param name="rrType">The numerical type of the resource record to be queried for (e.g. <see cref="DnsServiceType.PTR"/>, <see cref="DnsServiceType.SRV"/>, etc).</param>
    /// <param name="rrClass">The class of the resource record (usually <see cref="DnsServiceClass.IN"/>).</param>
    /// <param name="callback">The function to be called when a result is found, or if the call asynchronously fails.</param>
    /// <param name="context">An application context pointer which is passed to the callback function (may be NULL).</param>
    /// <returns>
    /// Returns <see cref="DnsServiceError.NoError"/> on success (any subsequent, asynchronous errors are delivered to the callback),
    /// otherwise returns an error code indicating the error that occurred (the callback is never invoked and the <see cref="DnsServiceRef"/> is not initialized).
    /// </returns>
    [DllImport(DllName, EntryPoint = "DNSServiceQueryRecord")]
    public static extern DnsServiceError DnsServiceQueryRecord(out DnsServiceRef sdRef, DnsServiceFlags flags, uint interfaceIndex, string fullName, DnsServiceType rrType, DnsServiceClass rrClass, DnsServiceQueryRecordReply callback, IntPtr context);

    [DllImport(DllName, EntryPoint = "DNSServiceReconfirmRecord")]
    public static extern void DnsServiceReconfirmRecord(DnsServiceFlags flags, uint interfaceIndex);

    public delegate void DnsServiceResolveReply(DnsServiceRef sdRef, DnsServiceFlags flags, uint interfaceIndex, DnsServiceError errorCode, string fullName, string hostTarget, ushort port, ushort txtLen, IntPtr txtRecord, IntPtr context);

    [DllImport(DllName, EntryPoint = "DNSServiceResolve", CharSet = CharSet.Ansi)]
    public static extern DnsServiceError DnsServiceResolve(out DnsServiceRef sdRef, DnsServiceFlags flags, uint interfaceIndex, string name, string regType, string domain, DnsServiceResolveReply callback, IntPtr context);

    /// <summary>
    /// Callback of <see cref="DnsServiceBrowse"/>.
    /// </summary>
    /// <param name="sdRef">The <see cref="DnsServiceRef"/> initialized by <see cref="DnsServiceBrowse"/>.</param>
    /// <param name="flags">Possible values are <see cref="DnsServiceFlags.MoreComing"/> and <see cref="DnsServiceFlags.Add"/>.</param>
    /// <param name="interfaceIndex">The interface on which the service is advertised. This index should be passed to <see cref="DnsServiceResolve"/> when resolving the service.</param>
    /// <param name="errorCode">Will be <see cref="DnsServiceError.NoError"/> on success, otherwise will indicate the failure that occurred. Other parameters are undefined on failure.</param>
    /// <param name="serviceName">The discovered service name. This name should be displayed to the user, and stored for subsequent use in the <see cref="DnsServiceResolve"/> call.</param>
    /// <param name="regType">
    /// The service type, which is usually (but not always) the same as was passed to <see cref="DnsServiceBrowse"/>. One case where the discovered service type may not be the same as the requested
    /// service type is when using subtypes. The client may want to browse for only those ftp servers that allow anonymous connections. The client will pass the string <c>_ftp._tcp,_anon</c> to
    /// <see cref="DnsServiceBrowse"/>, but the type of the service that's discovered is simply <c>_ftp._tcp</c>. The <c>regType</c> for each discovered service instance should be stored along with
    /// the name, so that it can be passed to <see cref="DnsServiceResolve"/> when the service is later resolved.
    /// </param>
    /// <param name="replyDomain">
    /// The domain of the discovered service instance. This may or may not be the same as the domain that was passed to <see cref="DnsServiceBrowse"/>. The domain for each discovered service
    /// instance should be stored along with the name, so that it can be passed to <see cref="DnsServiceResolve"/>  when the service is later resolved.
    /// </param>
    /// <param name="context">The context pointer that was passed to the callout.</param>
    public delegate void DnsServiceBrowseReply(DnsServiceRef sdRef, DnsServiceFlags flags, uint interfaceIndex, DnsServiceError errorCode, string serviceName, string regType, string replyDomain, IntPtr context);

    /// <summary>
    /// Starts browsing for services.
    /// </summary>
    /// <param name="sdRef">
    /// If the call succeeds then it initializes the <see cref="DnsServiceRef"/>, returns <see cref="DnsServiceError.NoError"/>,
    /// and the browse operation will run indefinitely until the client terminates it by passing this <see cref="DnsServiceRef"/> to <see cref="DnsServiceRefDeallocate"/>.
    /// </param>
    /// <param name="flags">Currently ignored, reserved for future use.</param>
    /// <param name="interfaceIndex">
    /// If non-zero, specifies the interface on which to browse for services (the index for a given interface is determined via the <c>if_nametoindex</c> family of calls.)
    /// Most applications will pass <see cref="DnsServiceInterfaceIndexAny"/> to browse on all available interfaces.
    /// </param>
    /// <param name="regType">
    /// The service type being browsed for followed by the protocol, separated by a dot (e.g. <c>_ftp._tcp</c>). The transport protocol must be <c>_tcp</c> or <c>_udp</c>.
    /// A client may optionally specify a single subtype to perform filtered browsing: e.g. browsing for <c>_primarytype._tcp,_subtype</c> will discover only those
    /// instances of <c>_primarytype._tcp</c> that were registered specifying <c>_subtype</c> in their list of registered subtypes.
    /// </param>
    /// <param name="domain">
    /// If non <see langword="null"/>, specifies the domain on which to browse for services. Most applications will not specify a domain, instead browsing on the default domain(s).
    /// </param>
    /// <param name="callback">The function to be called when an instance of the service being browsed for is found, or if the call asynchronously fails.</param>
    /// <param name="context">An application context pointer which is passed to the callback function (may be NULL).</param>
    /// <returns>
    /// Returns <see cref="DnsServiceError.NoError"/> on success (any subsequent, asynchronous errors are delivered to the callback), otherwise returns an error code indicating
    /// the error that occurred (the callback is not invoked and the <see cref="DnsServiceRef"/> is not initialized).
    /// </returns>
    [DllImport(DllName, EntryPoint = "DNSServiceBrowse")]
    public static extern DnsServiceError DnsServiceBrowse(out DnsServiceRef sdRef, DnsServiceFlags flags, uint interfaceIndex, string regType, string? domain, DnsServiceBrowseReply callback, IntPtr context);

    /// <summary>
    /// Read a reply from the daemon, calling the appropriate application callback. This call will block until the daemon's response is received. Use <see cref="DnsServiceRefSockFd"/> in conjunction
    /// with a run loop or <c>select()</c> to determine the presence of a response from the server before calling this function to process the reply without blocking. Call this function
    /// at any point if it is acceptable to block until the daemon's response arrives. Note that the client is responsible for ensuring that <c>DnsServiceProcessResult</c> is called whenever
    /// there is a reply from the daemon - the daemon may terminate its connection with a client that does not process the daemon's responses.
    /// </summary>
    /// <param name="sdRef">A <see cref="DnsServiceRef"/> initialized by any of the DnsService calls that take a callback parameter.</param>
    /// <returns>Returns <see cref="DnsServiceError.NoError"/> on success, otherwise returns an error code indicating the specific failure that occurred.</returns>
    [DllImport(DllName, EntryPoint = "DNSServiceProcessResult")]
    public static extern DnsServiceError DnsServiceProcessResult(DnsServiceRef sdRef);

    /// <summary>
    /// Access underlying Unix domain socket for an initialized <see cref="DnsServiceRef"/>.
    /// The DNS Service Discovery implementation uses this socket to communicate between the client and the mDNSResponder daemon. The application MUST NOT directly read from or write to this socket.
    /// Access to the socket is provided so that it can be used as a kqueue event source, a CFRunLoop event source, in a <c>select()</c> loop, etc. When the underlying event management subsystem
    /// (kqueue/select/CFRunLoop etc.) indicates to the client that data is available for reading on the socket, the client should call <see cref="DnsServiceProcessResult"/>, which will extract the
    /// daemon's reply from the socket, and pass it to the appropriate application callback. By using a run loop or <c>select()</c>, results from the daemon can be processed asynchronously.
    /// Alternatively, a client can choose to fork a thread and have it loop calling <see cref="DnsServiceProcessResult"/>. If <see cref="DnsServiceProcessResult"/> is called when no data is
    /// available for reading on the socket, it will block until data does become available, and then process the data and return to the caller. When data arrives on the socket, the client is
    /// responsible for calling <see cref="DnsServiceProcessResult"/> in a timely fashion -- if the client allows a large backlog of data to build up the daemon may terminate the connection.
    /// </summary>
    /// <param name="sdRef">A <see cref="DnsServiceRef"/> initialized by any of the DnsService calls.</param>
    /// <returns>The <see cref="DnsServiceRef"/> underlying socket descriptor, or <c>-1</c> on error.</returns>
    [DllImport(DllName, EntryPoint = "DNSServiceRefSockFD")]
    public static extern int DnsServiceRefSockFd(DnsServiceRef sdRef);

    /// <summary>
    /// <para>
    /// Terminate a connection with the daemon and free memory associated with the DNSServiceRef.
    /// Any services or records registered with this DNSServiceRef will be deregistered. Any Browse, Resolve, or Query operations called with this reference will be terminated.
    /// </para>
    /// <para>
    /// Note: If the reference's underlying socket is used in a run loop or select() call, it should be removed BEFORE <see cref="DnsServiceRefDeallocate"/> is called,
    /// as this function closes the reference's socket.
    /// </para>
    /// <para>
    /// Note: If the reference was initialized with <see cref="DnsServiceCreateConnection"/>, any <see cref="DnsRecordRef"/> created via this reference will be
    /// invalidated by this call - the resource records are deregistered, and their <see cref="DnsRecordRef"/> may not be used in subsequent functions.
    /// Similarly, if the reference was initialized with <see cref="DnsServiceRegister"/>, and an extra resource record was added to the service via <see cref="DnsServiceAddRecord"/>,
    /// the <see cref="DnsRecordRef"/> created by the Add() call is invalidated when this function is called - the <see cref="DnsRecordRef"/> may not be used in subsequent functions.
    /// </para>
    /// </summary>
    /// <param name="sdRef">A <see cref="DnsServiceRef"/> initialized by any of the DnsService calls.</param>
    [DllImport(DllName, EntryPoint = "DNSServiceRefDeallocate")]
    public static extern void DnsServiceRefDeallocate(DnsServiceRef sdRef);

    [DllImport(DllName, EntryPoint = "DNSServiceGetAddrInfo")]
    public static extern void DnsServiceGetAddrInfo(DnsServiceRef sdRef, DnsServiceFlags flags, uint interfaceIndex);

    [DllImport(DllName, EntryPoint = "DNSServiceCreateConnection")]
    public static extern DnsServiceError DnsServiceCreateConnection(out DnsServiceRef sdRef);

    /*
    public delegate void DNSServiceBrowseReply(DnsServiceRef sdRef, ServiceFlags flags, uint interfaceIndex, ServiceError errorCode, IntPtr serviceName, string regtype, string replyDomain, IntPtr context);

    public delegate void DNSServiceQueryRecordReply(DnsServiceRef sdRef, ServiceFlags flags, uint interfaceIndex, ServiceError errorCode, string fullname, ServiceType rrtype, ServiceClass rrclass, ushort rdlen, IntPtr rdata, uint ttl, IntPtr context);

    public delegate void DNSServiceRegisterReply(DnsServiceRef sdRef, ServiceFlags flags, ServiceError errorCode, IntPtr name, string regtype, string domain, IntPtr context);

    public delegate void DNSServiceResolveReply(DnsServiceRef sdRef, ServiceFlags flags, uint interfaceIndex, ServiceError errorCode, IntPtr fullname, string hosttarget, ushort port, ushort txtLen, IntPtr txtRecord, IntPtr context);


    [DllImport(BonjourDll)]
    public static extern void DNSServiceRefDeallocate(IntPtr sdRef);

    [DllImport(BonjourDll)]
    public static extern ServiceError DNSServiceProcessResult(IntPtr sdRef);

    [DllImport(BonjourDll)]
    public static extern int DNSServiceRefSockFD(IntPtr sdRef);

    [DllImport(BonjourDll, EntryPoint = "DNSServiceCreateConnection")]
    public static extern ServiceError DNSServiceCreateConnection(out DnsServiceRef sdRef);

    [DllImport(BonjourDll)]
    public static extern ServiceError DNSServiceBrowse(out DnsServiceRef sdRef, ServiceFlags flags, uint interfaceIndex, string regtype, string? domain, DNSServiceBrowseReply callback, IntPtr context);

    [DllImport(BonjourDll)]
    public static extern ServiceError DNSServiceResolve(out DnsServiceRef sdRef, ServiceFlags flags, uint interfaceIndex, byte[] name, string regtype, string domain, DNSServiceResolveReply callback, IntPtr context);

    [DllImport(BonjourDll)]
    public static extern ServiceError DNSServiceRegister(out DnsServiceRef sdRef, ServiceFlags flags, uint interfaceIndex, byte[] name, string regtype, string domain, string host, ushort port, ushort txtLen, byte[] txtRecord, DNSServiceRegisterReply callback, IntPtr context);

    [DllImport(BonjourDll)]
    public static extern ServiceError DNSServiceQueryRecord(out DnsServiceRef sdRef, ServiceFlags flags, uint interfaceIndex, string fullname, ServiceType rrtype, ServiceClass rrclass, DNSServiceQueryRecordReply callback, IntPtr context);

    [DllImport(BonjourDll)]
    public static extern void TXTRecordCreate(IntPtr txtRecord, ushort bufferLen, IntPtr buffer);

    [DllImport(BonjourDll)]
    public static extern void TXTRecordDeallocate(IntPtr txtRecord);

    [DllImport(BonjourDll)]
    public static extern ServiceError TXTRecordGetItemAtIndex(ushort txtLen, IntPtr txtRecord, ushort index, ushort keyBufLen, byte[] key, out byte valueLen, out IntPtr value);

    [DllImport(BonjourDll)]
    public static extern ServiceError TXTRecordSetValue(IntPtr txtRecord, byte[] key, sbyte valueSize, byte[] value);

    [DllImport(BonjourDll)]
    public static extern ServiceError TXTRecordRemoveValue(IntPtr txtRecord, byte[] key);

    [DllImport(BonjourDll)]
    public static extern ushort TXTRecordGetLength(IntPtr txtRecord);

    [DllImport(BonjourDll)]
    public static extern IntPtr TXTRecordGetBytesPtr(IntPtr txtRecord);

    [DllImport(BonjourDll)]
    public static extern ushort TXTRecordGetCount(ushort txtLen, IntPtr txtRecord);
    */
}