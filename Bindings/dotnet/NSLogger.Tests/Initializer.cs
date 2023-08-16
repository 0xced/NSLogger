[assembly: TestFramework("NSLogger.Tests.Initializer", "NSLogger.Tests")]

namespace NSLogger.Tests;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public class Initializer : XunitTestFramework
{
    private readonly ConcurrentDictionary<string, IntPtr> _nativeLibraries = new();

    public Initializer(IMessageSink messageSink) : base(messageSink)
    {
        NativeLibrary.SetDllImportResolver(typeof(Logger).Assembly, ResolveNativeLibrary);
    }

    /// <summary>
    /// Resolving libNSLogger.Native.dylib is required in unit tests because the test runner
    /// doesn't know where to find the dylib. When distributed as a NuGet package, with the dylib inside the
    /// <c>runtimes/osx/native</c> directory, the runtime knows how to automatically resolve the dylib.
    /// </summary>
    private IntPtr ResolveNativeLibrary(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        return _nativeLibraries.GetOrAdd(libraryName, name =>
        {
            var configuration = assembly.GetCustomAttribute<AssemblyConfigurationAttribute>()!.Configuration;
            var frameworkName = NuGetFramework.Parse(assembly.GetCustomAttribute<TargetFrameworkAttribute>()!.FrameworkName).GetShortFolderName();
            var nativeLibraryPath = GetNativeLibraryPath(configuration, frameworkName, name);
            return File.Exists(nativeLibraryPath) ? NativeLibrary.Load(nativeLibraryPath) : IntPtr.Zero;
        });
    }

    static string GetNativeLibraryPath(string configuration, string frameworkName, string libraryName, [CallerFilePath] string path = "")
    {
        return Path.GetFullPath(Path.Combine(Path.GetDirectoryName(path)!, "..", "NSLogger", "obj", configuration, frameworkName, $"lib{libraryName}.dylib"));
    }
}