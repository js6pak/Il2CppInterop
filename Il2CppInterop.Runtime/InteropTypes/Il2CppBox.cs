using Il2CppInterop.Bindings.Structs;

namespace Il2CppInterop.Runtime.InteropTypes;

public sealed unsafe class Il2CppBox<T> : Il2CppObjectBase where T : unmanaged
{
    public Il2CppBox(Il2CppObject* pointer) : base(pointer)
    {
    }
}
