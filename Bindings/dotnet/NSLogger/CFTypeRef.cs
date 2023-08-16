using static NSLogger.NativeMethods.CoreFoundation;

namespace NSLogger;

// ReSharper disable once InconsistentNaming
internal readonly struct CFTypeRef : IDisposable
{
    private readonly IntPtr _ref = IntPtr.Zero;

    public static implicit operator IntPtr(CFTypeRef s) => s._ref;

    public CFTypeRef(string? value)
    {
        if (value != null)
        {
            _ref = CFStringCreateWithCharacters(IntPtr.Zero, value, value.Length);
        }
    }

    public CFTypeRef(ReadOnlySpan<byte> value)
    {
        unsafe
        {
            // The preferred pattern is to use unmanaged pointer in the PInvoke signature and use fixed block to call it:
            // See https://github.com/dotnet/runtime/issues/27091#issuecomment-411475536
            fixed (byte *ptr = value)
            {
                _ref = CFDataCreate(IntPtr.Zero, ptr, value.Length);
            }
        }
    }

    public void Dispose()
    {
        if (_ref != IntPtr.Zero)
        {
            CFRelease(_ref);
        }
    }
}