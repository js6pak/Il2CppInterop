using AssetRipper.VersionUtilities;
using Il2CppInterop.Bindings;
using Il2CppInterop.Runtime.Hooks;
using Il2CppInterop.Runtime.Injection;

namespace Il2CppInterop.Runtime;

public sealed record RuntimeConfiguration(UnityVersion UnityVersion, IDetourProvider DetourProvider);

public sealed class Il2CppInteropRuntime
{
    private static Il2CppInteropRuntime? _instance;

    private Il2CppInteropRuntime(RuntimeConfiguration configuration)
    {
        UnityVersion = configuration.UnityVersion;
        DetourProvider = configuration.DetourProvider;
    }

    public static Il2CppInteropRuntime Instance
    {
        get => _instance ?? throw new InvalidOperationException("Il2CppInteropRuntime has not been initialized yet, call Il2CppInteropRuntime.Initialize first");
    }

    public UnityVersion UnityVersion { get; }

    public IDetourProvider DetourProvider { get; }

    internal GameAssemblyModule GameAssemblyModule { get; } = new();

    public static void Initialize(RuntimeConfiguration configuration)
    {
        if (_instance != null)
        {
            throw new InvalidOperationException("Il2CppInteropRuntime has been initialized already");
        }

        UnityVersionHandler.Initialize(configuration.UnityVersion);
        _instance = new Il2CppInteropRuntime(configuration);
    }
}
