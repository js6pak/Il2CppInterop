using HarmonyLib.Public.Patching;

namespace Il2CppInterop.HarmonySupport;

public static class HarmonySupport
{
    public static void Enable()
    {
        PatchManager.ResolvePatcher += TryResolve;
    }

    public static void Disable()
    {
        PatchManager.ResolvePatcher -= TryResolve;
    }

    private static void TryResolve(object sender, PatchManager.PatcherResolverEventArgs args)
    {
        throw new NotImplementedException();
    }
}
