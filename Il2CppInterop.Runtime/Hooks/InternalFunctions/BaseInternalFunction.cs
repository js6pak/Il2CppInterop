namespace Il2CppInterop.Runtime.Hooks.InternalFunctions;

internal abstract class BaseInternalFunction
{
    public abstract string Name { get; }

    public abstract IntPtr Find(X86FunctionScanner functionScanner);
}
