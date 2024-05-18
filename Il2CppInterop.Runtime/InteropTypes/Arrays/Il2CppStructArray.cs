using Il2CppInterop.Bindings.Structs;
using Il2CppInterop.Runtime.InteropTypes.Stores;

namespace Il2CppInterop.Runtime.InteropTypes.Arrays;

public unsafe class Il2CppStructArray<T> : Il2CppArrayBase<T> where T : unmanaged
{
    static Il2CppStructArray()
    {
        Il2CppClassPointerStore<Il2CppStructArray<T>>.Pointer = Il2CppClassPointerStore<Il2CppArrayBase<T>>.Pointer;
    }

    public Il2CppStructArray(Il2CppArray* pointer) : base(pointer)
    {
    }

    public Il2CppStructArray(int size) : base(size)
    {
    }

    public Il2CppStructArray(ReadOnlySpan<T> span) : base(span.Length)
    {
        span.CopyTo(Span);
    }

    public Il2CppStructArray(T[] array) : this(new ReadOnlySpan<T>(array))
    {
    }

    public new T* StartPointer => (T*)base.StartPointer;

    public Span<T> Span => new(StartPointer, Length);

    public override T this[int index]
    {
        get
        {
            EnsureIndexIsInBounds(index);
            return StartPointer[index];
        }
        set
        {
            EnsureIndexIsInBounds(index);
            StartPointer[index] = value;
        }
    }
}
