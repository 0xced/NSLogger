namespace NSLoggerSharp;

internal enum PartKey : byte
{
    MessageType = 0,

    /// <summary>
    /// Seconds component of timestamp.
    /// </summary>
    TimestampSeconds = 1,

    /// <summary>
    /// Milliseconds component of timestamp (optional, mutually exclusive with <see cref="TimestampMicroseconds"/>).
    /// </summary>
    TimestampMilliseconds = 2,

    /// <summary>
    /// Microseconds component of timestamp (optional, mutually exclusive with <see cref="TimestampMilliseconds"/>).
    /// </summary>
    TimestampMicroseconds = 3,
    ThreadId = 4,
    Tag = 5,
    Level = 6,
    Message = 7,

    /// <summary>
    /// Messages containing an image should also contain a part with the image width.
    /// (This is mainly for the desktop viewer to compute the cell size without having to immediately decode the image)
    /// </summary>
    ImageWidth = 8,

    /// <summary>
    /// Messages containing an image should also contain a part with the image height.
    /// (This is mainly for the desktop viewer to compute the cell size without having to immediately decode the image)
    /// </summary>
    ImageHeight = 9,

    /// <summary>
    /// The sequential number of this message which indicates the order in which messages are generated.
    /// </summary>
    MessageSeq = 10,

    FileName = 11,
    LineNumber = 12,
    FunctionName = 13,

    ClientName = 20,
    ClientVersion = 21,
    OsName = 22,
    OsVersion = 23,

    /// <summary>
    /// For iPhone, device model (i.e 'iPhone', 'iPad', etc)
    /// </summary>
    ClientModel = 24,

    /// <summary>
    /// For remote device identification, part of LOGMSG_TYPE_CLIENTINFO
    /// </summary>
    UniqueId = 25,
}