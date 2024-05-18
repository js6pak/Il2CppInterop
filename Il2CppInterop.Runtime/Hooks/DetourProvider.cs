namespace Il2CppInterop.Runtime.Hooks;

public interface IDetour : IDisposable
{
    nint Target { get; }
    nint Detour { get; }
}

public interface IDetourProvider
{
    IDetour CreateAndApply<T>(nint original, T target, out T trampoline) where T : Delegate;
}
