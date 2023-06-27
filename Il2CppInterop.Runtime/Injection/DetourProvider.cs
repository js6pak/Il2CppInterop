using System.Runtime.InteropServices;

namespace Il2CppInterop.Runtime.Injection;

public interface IDetour : IDisposable
{
    nint Original { get; }
    nint Target { get; }
    nint OriginalTrampoline { get; }

    void Apply();
}

public interface IDetourProvider
{
    IDetour Create<TDelegate>(nint original, TDelegate target) where TDelegate : Delegate;
}

internal static class Detour
{
    public static T Apply<T>(nint original, T target) where T : Delegate
    {
        var detour = Il2CppInteropRuntime.Instance.DetourProvider.Create(original, target);
        detour.Apply();
        return Marshal.GetDelegateForFunctionPointer<T>(detour.OriginalTrampoline);
    }
}
