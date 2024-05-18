using Il2CppInterop.Bindings;
using Il2CppInterop.Bindings.Structs;

namespace Il2CppInterop.Runtime.InteropTypes;

public abstract unsafe class Il2CppObjectBase : IEquatable<Il2CppObjectBase>
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
            if (handleTarget == null)
                throw new ObjectCollectedException("Object was garbage collected in IL2CPP domain");
            return handleTarget;
        }
        internal set
        {
            _gcHandle = Il2CppGCHandle.New(value, false);
        }
    }

    public bool WasCollected => _gcHandle.Target == null;

    ~Il2CppObjectBase()
    {
        _gcHandle.Free();

        if (OptionalFeatures.Il2CppObjectPooling.IsEnabled)
        {
            Il2CppObjectPool.Return(this);
            GC.ReRegisterForFinalize(this);
        }
    }

    public bool Equals(Il2CppObjectBase? other)
    {
        if (other is null) return false;
        return Pointer == other.Pointer;
    }

    public override bool Equals(object? obj)
    {
        return obj is Il2CppObjectBase other && Equals(other);
    }

    public override int GetHashCode()
    {
        return ((IntPtr)Pointer).GetHashCode();
    }
}
