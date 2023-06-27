using Il2CppInterop.Bindings.Structs;
using Il2CppInterop.Runtime.InteropTypes;

namespace Il2CppInterop.Runtime;

// TODO delete this

public sealed unsafe class InvokeTest : Il2CppObjectBase
{
    private static readonly Il2CppMethod* Il2CppMethodInfo_100663301_Nothing;

    public static void Nothing()
    {
        Il2CppMethodInfo_100663301_Nothing->Invoke(null, null);
    }

    public static int Sum(int a, int b)
    {
        Il2CppObject** @params = stackalloc Il2CppObject*[2];
        @params[0] = Il2CppObject.FakeBox(&a);
        @params[1] = Il2CppObject.FakeBox(&b);
        return Il2CppMethodInfo_100663301_Nothing->Invoke(null, @params)->Unbox<int>();
    }

    public static int DirectSum(int a, int b)
    {
        var method = (delegate *unmanaged<int, int, Il2CppMethod*, int>)Il2CppMethodInfo_100663301_Nothing->MethodPointer;
        return method(a, b, Il2CppMethodInfo_100663301_Nothing);
    }

    public int InstanceSum(int a, int b, InvokeTest? c)
    {
        Il2CppObject** @params = stackalloc Il2CppObject*[2];
        @params[0] = Il2CppObject.FakeBox(&a);
        @params[1] = Il2CppObject.FakeBox(&b);
        @params[2] = c != null ? c.Pointer : null;
        return Il2CppMethodInfo_100663301_Nothing->Invoke(Pointer, @params)->Unbox<int>();
    }

    public InvokeTest(Il2CppObject* pointer) : base(pointer)
    {
    }
}
