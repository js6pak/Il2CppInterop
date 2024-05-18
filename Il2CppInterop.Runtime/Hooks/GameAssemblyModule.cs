using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Il2CppInterop.Runtime.Hooks;

internal sealed unsafe class GameAssemblyModule
{
    public static string Name { get; }

    public ProcessModule Module { get; }

    public IntPtr Handle { get; }

    public UnmanagedMemoryStream Stream { get; }

    public GameAssemblyModule()
    {
        Module = Process.GetCurrentProcess()
            .Modules.OfType<ProcessModule>()
            .Single(m => m.ModuleName == Name);

        Handle = NativeLibrary.Load(Module.FileName);

        Stream = new UnmanagedMemoryStream((byte*)Module.BaseAddress, Module.ModuleMemorySize);
    }

    static GameAssemblyModule()
    {
        if (OperatingSystem.IsWindows())
        {
            Name = "GameAssembly.dll";
        }
        else if (OperatingSystem.IsLinux())
        {
            Name = "GameAssembly.so";
        }
        else if (OperatingSystem.IsAndroid())
        {
            Name = "libil2cpp.so";
        }
        else
        {
            throw new PlatformNotSupportedException();
        }
    }
}
