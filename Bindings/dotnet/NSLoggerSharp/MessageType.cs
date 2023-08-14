namespace NSLoggerSharp;

internal enum MessageType
{
    /// <summary>
    /// A standard log message.
    /// </summary>
    Log = 0,

    /// <summary>
    /// The start of a "block" (a group of log entries).
    /// </summary>
    BlockStart = 1,

    /// <summary>
    /// The end of the last started "block".
    /// </summary>
    BlockEnd = 2,

    /// <summary>
    /// Information about the client app.
    /// </summary>
    ClientInfo = 3,

    /// <summary>
    /// Pseudo-message on the desktop side to identify client disconnects.
    /// </summary>
    Disconnect = 4,

    /// <summary>
    /// Pseudo-message that defines a "mark" that users can place in the log flow.
    /// </summary>
    Mark = 5,
}