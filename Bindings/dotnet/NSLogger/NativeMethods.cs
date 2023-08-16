namespace NSLogger;

internal static class NativeMethods
{
    [SuppressMessage("Globalization", "CA2101:Specify marshaling for P/Invoke string arguments", Justification = "False positive, see https://github.com/dotnet/roslyn-analyzers/issues/5479")]
    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Interoperability")]
    internal static class NSLogger
    {
        private const string NativeLib = "NSLogger.Native";

        [DllImport(NativeLib)]
        public static extern IntPtr LoggerInit();

        [DllImport(NativeLib)]
        public static extern void LoggerStop(IntPtr logger);

        [DllImport(NativeLib)]
        public static extern void LoggerSetupBonjour(IntPtr logger, IntPtr /* CFStringRef */ bonjourServiceType, IntPtr /* CFStringRef */ bonjourServiceName);

        [DllImport(NativeLib)]
        public static extern void LoggerSetViewerHost(IntPtr logger, IntPtr /* CFStringRef */ hostName, uint port);

        [DllImport(NativeLib)]
        public static extern void LoggerSetOptions(IntPtr logger, uint options);

        [DllImport(NativeLib)]
        public static extern void LoggerSetClient(IntPtr logger, IntPtr /* CFStringRef */ clientName, IntPtr /* CFStringRef */ clientVersion);

        [DllImport(NativeLib)]
        public static extern void LoggerSetTimestamp(IntPtr logger, timeval timestamp);

        [DllImport(NativeLib)]
        public static extern void LoggerFlush(IntPtr logger, bool waitForConnection);

        [DllImport(NativeLib, CharSet = CharSet.Ansi)]
        public static extern void LogMessageRawToF(IntPtr logger, string? fileName, int lineNumber, string? functionName, IntPtr /* NSString */ domain, int level, IntPtr /* NSString */ message);

        [DllImport(NativeLib, CharSet = CharSet.Ansi)]
        public static extern void LogDataToF(IntPtr logger, string? fileName, int lineNumber, string? functionName, IntPtr /* NSString */ domain, int level, IntPtr /* NSData */ data);

        [DllImport(NativeLib, CharSet = CharSet.Ansi)]
        public static extern void LogImageDataToF(IntPtr logger, string? fileName, int lineNumber, string? functionName, IntPtr /* NSString */ domain, int level, int width, int height, IntPtr /* NSData */ data);

        [DllImport(NativeLib)]
        public static extern IntPtr /* NSString */ NSLoggerGetCurrentThreadName();

        [DllImport(NativeLib)]
        public static extern void NSLoggerSetCurrentThreadName(IntPtr /* NSString */ threadName);

        [StructLayout(LayoutKind.Sequential)]
        [SuppressMessage("ReSharper", "PrivateFieldCanBeConvertedToLocalVariable", Justification = "Interoperability")]
        [SuppressMessage("ReSharper", "IdentifierTypo", Justification = "Interoperability")]
        public struct timeval
        {
            public timeval(DateTimeOffset date)
            {
                var milliseconds = date.ToUnixTimeMilliseconds();
                tv_sec = milliseconds / 1000;
                tv_usec = (int)(milliseconds % 1000) * 1000;
            }

            private readonly long tv_sec;
            private readonly int tv_usec;
        }
    }

    internal static class CoreFoundation
    {
        private const string CoreFoundationFramework = "/System/Library/Frameworks/CoreFoundation.framework/Versions/A/CoreFoundation";

        [DllImport(CoreFoundationFramework, CharSet = CharSet.Unicode)]
        public static extern IntPtr CFStringCreateWithCharacters(IntPtr allocator, string chars, long numChars);

        [DllImport(CoreFoundationFramework)]
        public static extern unsafe IntPtr CFDataCreate(IntPtr allocator, byte *bytes, long length);

        [Conditional("DEBUG")]
        [DllImport(CoreFoundationFramework)]
        public static extern void CFShow(IntPtr obj);

        [DllImport(CoreFoundationFramework)]
        public static extern void CFRelease(IntPtr cf);
    }
}