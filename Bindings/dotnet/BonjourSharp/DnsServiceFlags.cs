using System;

namespace BonjourSharp;

/// <summary>
/// Most DNS Service Discovery API functions and callbacks include a <c>DnsServiceFlags</c> parameter.
/// As a general rule, any given bit in the 32-bit flags field has a specific fixed meaning, regardless of the function or callback being used.
/// For any given function or callback, typically only a subset of the possible flags are meaningful, and all others should be zero.
/// The discussion section for each API call describes which flags are valid for that call and callback. In some cases, for a particular call,
/// it may be that no flags are currently defined, in which case the <c>DnsServiceFlags</c> parameter exists purely to allow future expansion.
/// In all cases, developers should expect that in future releases, it is possible that new flag values will be defined, and write code with this in mind.
/// For example, code that tests
///     <code>if (flags == DnsServiceFlags.Add) …</code>
/// will fail if, in a future release, another bit in the 32-bit flags field is also set.
/// The reliable way to test whether a particular bit is set is not with an equality test, but with the <see cref="Enum.HasFlag"/> method:
///     <code>if (flags.HasFlag(DnsServiceFlags.Add)) …</code>
/// </summary>
[Flags]
public enum DnsServiceFlags : uint
{
    /// <summary>
    /// <c>None</c> should be used for functions that don't need  any particular flag, or where flags are
    /// reserved for future use (such as <see cref="DnsServiceDiscovery.DnsServiceBrowse"/>).
    /// </summary>
    None = 0x0,

    /// <summary>
    /// <c>MoreComing</c> indicates to a callback that at least one more result is queued and will be delivered following immediately after this one.
    /// When the <c>MoreComing</c> flag is set, applications should not immediately update their UI, because this can result in a great deal of ugly
    /// flickering on the screen, and can waste a great deal of CPU time repeatedly updating the screen with content that is then immediately erased, over and over.
    /// Applications should wait until <c>MoreComing</c> is not set, and then update their UI when no more changes are imminent.
    /// When <c>MoreComing</c> is not set, that doesn't mean there will be no more answers EVER, just that there are no more answers immediately available right now at this instant.
    /// If more answers become available in the future they will be delivered as usual.
    /// </summary>
    MoreComing = 0x1,

    /// <summary>
    /// Flags for domain enumeration and browse/query reply callbacks.
    /// <see cref="Default"/> applies only to enumeration and is only valid in conjunction with <c>Add</c>.
    /// An enumeration callback with the <c>Add</c> flag NOT set indicates a "Remove", i.e. the domain is no longer valid.
    /// </summary>
    Add = 0x2,

    /// <summary>
    /// Flags for domain enumeration and browse/query reply callbacks.
    /// <c>Default</c> applies only to enumeration and is only valid in conjunction with <see cref="Add"/>.
    /// An enumeration callback with the <see cref="Add"/> flag NOT set indicates a "Remove", i.e. the domain is no longer valid.
    /// </summary>
    Default = 0x4,

    /// <summary>
    /// Flag for specifying renaming behavior on name conflict when registering non-shared records.
    /// By default, name conflicts are automatically handled by renaming the service. <c>NoAutoRename</c> overrides this behavior - with this flag set, name conflicts will result in a callback.
    /// The <c>NoAutoRename</c> flag is only valid if a name is explicitly specified when registering a service.
    /// </summary>
    NoAutoRename = 0x8,

    /// <summary>
    /// Flag for registering individual records on a connected <see cref="DnsServiceRef"/>.
    /// <c>Shared</c> indicates that there may be multiple records with this name on the network (e.g. <c>PTR</c> records).
    /// <see cref="Unique"/> indicates that the record's name is to be unique on the network (e.g. <c>SRV</c> records).
    /// </summary>
    Shared = 0x10,

    /// <summary>
    /// Flag for registering individual records on a connected <see cref="DnsServiceRef"/>.
    /// <see cref="Shared"/> indicates that there may be multiple records with this name on the network (e.g. <c>PTR</c> records).
    /// <c>Unique</c> indicates that the record's name is to be unique on the network (e.g. <c>SRV</c> records).
    /// </summary>
    Unique = 0x20,

    /// <summary>
    /// Flags for specifying domain enumeration type in <see cref="DnsServiceDiscovery.DnsServiceEnumerateDomains"/>.
    /// <c>BrowseDomains</c> enumerates domains recommended for browsing.
    /// </summary>
    BrowseDomains = 0x40,

    /// <summary>
    /// Flags for specifying domain enumeration type in <see cref="DnsServiceDiscovery.DnsServiceEnumerateDomains"/>.
    /// <c>RegistrationDomains</c> enumerates domains recommended for registration.
    /// </summary>
    RegistrationDomains = 0x80,

    /// <summary>
    /// Flag for creating a long-lived unicast query for the <see cref="DnsServiceDiscovery.DnsServiceQueryRecord"/> call.
    /// </summary>
    LongLivedQuery = 0x100,

    /// <summary>
    /// Flag for creating a record for which we will answer remote queries (queries from hosts more than one hop away; hosts not directly connected to the local link).
    /// </summary>
    AllowRemoteQuery = 0x200,

    /// <summary>
    /// Flag for signifying that a query or registration should be performed exclusively via multicast DNS,
    /// even for a name in a domain (e.g. foo.apple.com.) that would normally imply unicast DNS.
    /// </summary>
    ForceMulticast = 0x400,

    /// <summary>
    /// Flag for signifying a "stronger" variant of an operation.
    /// Currently defined only for <see cref="DnsServiceDiscovery.DnsServiceReconfirmRecord"/>, where it forces a record to be removed from the
    /// cache immediately, instead of querying for a few seconds before concluding that the record is no longer valid and then removing it.
    /// This flag should be used with caution because if a service browsing <c>PTR</c> record is indeed still valid on the network, forcing
    /// its removal will result in a user-interface flap -- the discovered service instance will disappear, and then re-appear moments later.
    /// </summary>
    Force = 0x800,

    /// <summary>
    /// Flag for returning intermediate results. For example, if a query results in an authoritative NXDomain (name does not exist) then that result is returned to the client.
    /// However the query is not implicitly cancelled -- it remains active and if the answer subsequently changes (e.g. because a VPN tunnel is subsequently established)
    /// then that positive result will still be returned to the client. Similarly, if a query results in a <c>CNAME</c> record, then in addition to following the <c>CNAME</c> referral,
    /// the intermediate <c>CNAME</c> result is also returned to the client. When this flag is not set, NXDomain errors are not returned, and <c>CNAME</c> records are followed silently
    /// without informing the client of the intermediate steps.
    /// </summary>
    ReturnIntermediates = 0x1000,

    /// <summary>
    /// A service registered with the <c>NonBrowsable</c> flag set can be resolved using <see cref="DnsServiceDiscovery.DnsServiceResolve"/>,
    /// but will not be discoverable using <see cref="DnsServiceDiscovery.DnsServiceBrowse"/>.
    /// This is for cases where the name is actually a GUID; it is found by other means; there is no end-user benefit to browsing to find a long list of opaque GUIDs.
    /// Using the NonBrowsable flag creates <c>SRV</c> + <c>TXT</c> without the cost of also advertising an associated <c>PTR</c> record.
    /// </summary>
    NonBrowsable = 0x2000,

    /// <summary>
    /// For efficiency, clients that perform many concurrent operations may want to use a single Unix Domain Socket connection with the background daemon, instead of having a
    /// separate connection for each independent operation. To use this mode, clients first call DNSServiceCreateConnection(&amp;MainRef) to initialize the main DNSServiceRef.
    /// For each subsequent operation that is to share that same connection, the client copies the MainRef, and then passes the address of that copy, setting the ShareConnection flag
    /// to tell the library that this DNSServiceRef is not a typical uninitialized DNSServiceRef; it's a copy of an existing DNSServiceRef whose connection information should be reused.
    ///
    /// For example:
    ///
    /// <code>
    /// DNSServiceErrorType error;
    /// DNSServiceRef MainRef;
    /// error = DNSServiceCreateConnection(&amp;MainRef);
    /// if (error) ...
    /// DNSServiceRef BrowseRef = MainRef;  // Important: COPY the primary DNSServiceRef first...
    /// error = DNSServiceBrowse(&amp;BrowseRef, ShareConnection, ...); // then use the copy
    /// if (error) ...
    /// ...
    /// DNSServiceRefDeallocate(BrowseRef); // Terminate the browse operation
    /// DNSServiceRefDeallocate(MainRef);   // Terminate the shared connection
    /// </code>
    ///
    /// <remarks>
    /// <para>
    /// 1. Collective <see cref="MoreComing"/> flag
    /// </para>
    /// <para>
    /// When callbacks are invoked using a shared DNSServiceRef, the
    /// MoreComing flag applies collectively to *all* active
    /// operations sharing the same parent DNSServiceRef. If the MoreComing flag is
    /// set it means that there are more results queued on this parent DNSServiceRef,
    /// but not necessarily more results for this particular callback function.
    /// The implication of this for client programmers is that when a callback
    /// is invoked with the MoreComing flag set, the code should update its
    /// internal data structures with the new result, and set a variable indicating
    /// that its UI needs to be updated. Then, later when a callback is eventually
    /// invoked with the MoreComing flag not set, the code should update *all*
    /// stale UI elements related to that shared parent DNSServiceRef that need
    /// updating, not just the UI elements related to the particular callback
    /// that happened to be the last one to be invoked.
    /// </para>
    ///
    /// <para>
    /// 2. Canceling operations and <see cref="MoreComing"/>
    /// </para>
    /// <para>
    /// Whenever you cancel any operation for which you had deferred UI updates
    /// waiting because of a MoreComing flag, you should perform
    /// those deferred UI updates. This is because, after cancelling the operation,
    /// you can no longer wait for a callback *without* MoreComing set, to tell
    /// you do perform your deferred UI updates (the operation has been canceled,
    /// so there will be no more callbacks). An implication of the collective
    /// MoreComing flag for shared connections is that this
    /// guideline applies more broadly -- any time you cancel an operation on
    /// a shared connection, you should perform all deferred UI updates for all
    /// operations sharing that connection. This is because the MoreComing flag
    /// might have been referring to events coming for the operation you canceled,
    /// which will now not be coming because the operation has been canceled.
    /// </para>
    ///
    /// <para>
    /// 3. Only share DNSServiceRef's created with DNSServiceCreateConnection
    /// </para>
    /// <para>
    /// Calling DNSServiceCreateConnection(&amp;ref) creates a special shareable DNSServiceRef.
    /// DNSServiceRef's created by other calls like DNSServiceBrowse() or DNSServiceResolve()
    /// cannot be shared by copying them and using ShareConnection.
    /// </para>
    ///
    /// <para>
    /// 4. Don't Double-Deallocate
    /// </para>
    /// <para>
    /// Calling DNSServiceRefDeallocate(ref) for a particular operation's DNSServiceRef terminates
    /// just that operation. Calling DNSServiceRefDeallocate(ref) for the main shared DNSServiceRef
    /// (the parent DNSServiceRef, originally created by DNSServiceCreateConnection(&amp;ref))
    /// automatically terminates the shared connection and all operations that were still using it.
    /// After doing this, DO NOT then attempt to deallocate any remaining subordinate DNSServiceRef's.
    /// The memory used by those subordinate DNSServiceRef's has already been freed, so any attempt
    /// to do a DNSServiceRefDeallocate (or any other operation) on them will result in accesses
    /// to freed memory, leading to crashes or other equally undesirable results.
    /// </para>
    ///
    /// <para>
    /// 5. Thread Safety
    /// </para>
    /// <para>
    /// The dns_sd.h API does not presuppose any particular threading model, and consequently
    /// does no locking of its own (which would require linking some specific threading library).
    /// If client code calls API routines on the same DNSServiceRef concurrently
    /// from multiple threads, it is the client's responsibility to use a mutex
    /// lock or take similar appropriate precautions to serialize those calls.
    /// </para>
    /// </remarks>
    /// </summary>
    ShareConnection = 0x4000,

    /// <summary>
    /// This flag is meaningful only in <see cref="DnsServiceDiscovery.DnsServiceQueryRecord"/> which suppresses unusable queries on the wire.
    /// If "hostname" is a wide-area unicast DNS hostname (i.e. not a ".local." name) but this host has no routable IPv6 address, then the call will not try to look
    /// up IPv6 addresses for "hostname", since any addresses it found would be unlikely to be of any use anyway. Similarly, if this host has no routable IPv4 address,
    /// the call will not try to look up IPv4 addresses for "hostname".
    /// </summary>
    SuppressUnusable = 0x8000,

    /// <summary>
    /// When <c>Timeout</c> is passed to <see cref="DnsServiceDiscovery.DnsServiceQueryRecord"/> or <see cref="DnsServiceDiscovery.DnsServiceGetAddrInfo"/>, the query is
    /// stopped after a certain number of seconds have elapsed. The time at which the query will be stopped is determined by the system and cannot be configured by the user.
    /// The query will be stopped irrespective of whether a response was given earlier or not. When the query is stopped, the callback will be called with an error code
    /// of <c>Timeout</c> and a NULL sockaddr will be returned for <see cref="DnsServiceDiscovery.DnsServiceGetAddrInfo"/> and zero length rdata will be returned
    /// for <see cref="DnsServiceDiscovery.DnsServiceQueryRecord"/>.
    /// </summary>
    Timeout = 0x10000,

    /// <summary>
    /// Include peer-to-peer interfaces when <see cref="DnsServiceDiscovery.DnsServiceInterfaceIndexAny"/> is specified.
    /// By default, specifying <see cref="DnsServiceDiscovery.DnsServiceInterfaceIndexAny"/> does not include peer-to-peer interfaces.
    /// </summary>
    IncludeP2P = 0x20000,

    /// <summary>
    /// This flag is meaningful only in <see cref="DnsServiceDiscovery.DnsServiceResolve"/>.
    /// When set, it tries to send a magic packet to wake up the client.
    /// </summary>
    WakeOnResolve = 0x40000,
}
