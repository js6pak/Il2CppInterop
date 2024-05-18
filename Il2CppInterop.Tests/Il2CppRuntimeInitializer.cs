using System.Runtime.InteropServices;
using AssetRipper.VersionUtilities;
using Il2CppInterop.Bindings;
using Il2CppInterop.Bindings.Structs;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Hooks;

namespace Il2CppInterop.Tests;

public sealed unsafe class Il2CppRuntimeInitializer : IDisposable
{
    private readonly ManualResetEventSlim _shutdownEvent = new();

    // TODO Replace with env variables or something
    public static string GamePath { get; } = "/home/js6pak/Development/BepInEx/TestGame/Build/linux-x64";
    public static UnityVersion GameUnityVersion { get; } = UnityVersion.Parse("2020.3.22f1");

    public IntPtr GameAssemblyHandle { get; }

    public static Action<string>? LogEvent { get; set; }

    public Il2CppRuntimeInitializer()
    {
        Console.WriteLine("Il2CppRuntimeInitializer");

        if (GameAssemblyHandle != default) return;

        GameAssemblyHandle = NativeLibrary.Load(Path.Combine(GamePath, GameAssemblyModule.Name));

        NativeLibrary.SetDllImportResolver(typeof(Il2CppRuntime).Assembly, (name, _, _) =>
        {
            if (name == "GameAssembly")
            {
                return GameAssemblyHandle;
            }

            return default;
        });

        Il2CppRuntime.RegisterLogCallback(&LogCallback);
        Il2CppRuntime.SetDataDir(Path.Combine(GamePath, "TestGame_Data", "il2cpp_data"));

        var initializedEvent = new ManualResetEventSlim();
        var il2CppThread = new Thread(() =>
        {
            Console.WriteLine("Initializing il2cpp runtime");
            if (Il2CppRuntime.Init("Il2CppInterop.Tests", GameUnityVersion) == 0)
            {
                throw new Exception("Failed to initialize il2cpp runtime");
            }

            initializedEvent.Set();
            _shutdownEvent.Wait();

            Console.WriteLine("Shutting down il2cpp runtime");
            Il2CppRuntime.Shutdown();
        })
        {
            Name = "IL2CPP Main",
        };
        il2CppThread.Start();
        initializedEvent.Wait();

        Il2CppInteropRuntime.Initialize(new RuntimeConfiguration(GameUnityVersion, null!));

        DebugLogHandler.Register();

        Console.WriteLine("Initialized il2cpp runtime");
    }

    public void Dispose()
    {
        _shutdownEvent.Set();
    }

    private static void Log(string message)
    {
        if (LogEvent != null)
        {
            LogEvent.Invoke(message);
        }
        else
        {
            Console.WriteLine(message);
        }
    }

    [UnmanagedCallersOnly]
    private static void LogCallback(char* rawMessage)
    {
        var message = Marshal.PtrToStringUTF8((IntPtr)rawMessage);
        if (message == null) return;

        Log(message);
    }

    private static class DebugLogHandler
    {
        public static void Register()
        {
            Il2CppRuntime.AddInternalCall("UnityEngine.DebugLogHandler::Internal_Log", (delegate* unmanaged<LogType, LogOption, Il2CppString*, Il2CppObject*, void>)&Internal_Log);
            Il2CppRuntime.AddInternalCall("UnityEngine.DebugLogHandler::Internal_LogException", (delegate* unmanaged<Il2CppException*, Il2CppObject*, void>)&Internal_LogException);
        }

        [UnmanagedCallersOnly]
        public static void Internal_Log(LogType level, LogOption options, Il2CppString* msg, Il2CppObject* obj)
        {
            Log($"[{nameof(DebugLogHandler)}] [{level}] {msg->ToString()}");
        }

        [UnmanagedCallersOnly]
        public static void Internal_LogException(Il2CppException* ex, Il2CppObject* obj)
        {
            Log($"[{nameof(DebugLogHandler)}] {ex->Format()}");
        }

        public enum LogType
        {
            Error,
            Assert,
            Warning,
            Log,
            Exception,
        }

        public enum LogOption
        {
            None,
            NoStacktrace,
        }
    }
}
