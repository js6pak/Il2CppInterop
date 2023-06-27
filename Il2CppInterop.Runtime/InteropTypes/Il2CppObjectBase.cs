using Il2CppInterop.Bindings;
using Il2CppInterop.Bindings.Structs;

namespace Il2CppInterop.Runtime.InteropTypes;

public abstract unsafe class Il2CppObjectBase
{
    private Il2CppGCHandle _gcHandle;

    protected Il2CppObjectBase(Il2CppObject* pointer)
    {
        Pointer = pointer;
    }

    public Il2CppObject* Pointer
    {
        get
        {
            var handleTarget = _gcHandle.Target;
            if (handleTarget == default)
                throw new ObjectCollectedException("Object was garbage collected in IL2CPP domain");
            return handleTarget;
        }
        internal set
        {
            _gcHandle = Il2CppGCHandle.New(value, false);
            Il2CppObjectPool.Add(this);
        }
    }

    public bool WasCollected => _gcHandle.Target == default;

    ~Il2CppObjectBase()
    {
        _gcHandle.Free();
        Il2CppObjectPool.Resurrect(this);
    }
}
