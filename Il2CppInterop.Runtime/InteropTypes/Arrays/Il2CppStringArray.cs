using Il2CppInterop.Bindings.Structs;

namespace Il2CppInterop.Runtime.InteropTypes.Arrays;

public unsafe class Il2CppStringArray : Il2CppArrayBase<string>
{
    static Il2CppStringArray()
    {
        Il2CppClassPointerStore<Il2CppStringArray>.Pointer = Il2CppClassPointerStore<Il2CppArrayBase<string>>.Pointer;
    }

    public Il2CppStringArray(Il2CppArray* pointer) : base(pointer)
    {
    }

    public Il2CppStringArray(int size) : base(size)
    {
    }

    public Il2CppStringArray(string[] array) : base(array)
    {
    }

    protected new Il2CppString** StartPointer => (Il2CppString**)base.StartPointer;

    public override string this[int index]
    {
        get
        {
            EnsureIndexIsInBounds(index);
            return StartPointer[index]->ToString();
        }
        set
        {
            EnsureIndexIsInBounds(index);
            StartPointer[index] = Il2CppString.From(value);
        }
    }
}
