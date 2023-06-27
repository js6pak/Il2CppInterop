using Il2CppInterop.Bindings.Structs;

namespace Il2CppInterop.Runtime.InteropTypes.Arrays;

public unsafe class Il2CppReferenceArray<T> : Il2CppArrayBase<T> where T : Il2CppObjectBase
{
    static Il2CppReferenceArray()
    {
        Il2CppClassPointerStore<Il2CppReferenceArray<T>>.Pointer = Il2CppClassPointerStore<Il2CppArrayBase<T>>.Pointer;
    }

    public Il2CppReferenceArray(Il2CppArray* pointer) : base(pointer)
    {
    }

    public Il2CppReferenceArray(int size) : base(size)
    {
    }

    public Il2CppReferenceArray(T[] array) : base(array)
    {
    }

    protected new Il2CppObject** StartPointer => (Il2CppObject**)base.StartPointer;

    public override T this[int index]
    {
        get
        {
            EnsureIndexIsInBounds(index);
            return Il2CppObjectPool.Get<T>(StartPointer[index]);
        }
        set
        {
            EnsureIndexIsInBounds(index);
            StartPointer[index] = value.Pointer;
        }
    }
}
