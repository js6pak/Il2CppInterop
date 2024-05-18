using Il2CppInterop.Bindings;
using Il2CppInterop.Bindings.Structs;
using Il2CppInterop.Bindings.Utilities;
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppInterop.Runtime.InteropTypes.Stores;

namespace Il2CppInterop.Runtime;

public static unsafe class LightReflection
{
    private static readonly Il2CppMethod* _makeGenericTypeMethodPointer;
    private static readonly Il2CppMethod* _makeGenericMethodMethodPointer;

    static LightReflection()
    {
        var type = Il2CppRuntime.GetClassFromName("mscorlib.dll", "System", "Type");
        Il2CppClassPointerStore<Il2CppReflectionType>.Pointer = type;
        _makeGenericTypeMethodPointer = type->GetMethodByNameOrThrow("MakeGenericType");

        var methodInfo = Il2CppRuntime.GetClassFromName("mscorlib.dll", "System.Reflection", "MethodInfo");
        Il2CppClassPointerStore<Il2CppReflectionMethod>.Pointer = methodInfo;
        _makeGenericMethodMethodPointer = methodInfo->GetMethodByNameOrThrow("MakeGenericMethod");
    }

    public static Il2CppReflectionType* MakeGenericType(Il2CppReflectionType* type, Il2CppStructArray<Pointer<Il2CppReflectionType>> typeArguments)
    {
        var virtualMethod = type->Object.GetVirtualMethod(_makeGenericTypeMethodPointer);
        var methodPointer = (delegate* unmanaged<Il2CppReflectionType*, Il2CppArray*, Il2CppMethod*, Il2CppReflectionType*>)virtualMethod->MethodPointer;
        return methodPointer(type, typeArguments.Pointer, virtualMethod);
    }

    public static Il2CppReflectionType* MakeGenericType(Il2CppReflectionType* type, params Il2CppReflectionType*[] typeArguments)
    {
        var array = new Il2CppStructArray<Pointer<Il2CppReflectionType>>(typeArguments.Length);
        for (var i = 0; i < typeArguments.Length; i++)
        {
            array[i] = typeArguments[i];
        }

        return MakeGenericType(type, array);
    }

    public static Il2CppClass* MakeGenericClass(Il2CppClass* type, params Il2CppClass*[] classArguments)
    {
        var typeArguments = new Il2CppReflectionType*[classArguments.Length];
        for (var i = 0; i < classArguments.Length; i++)
        {
            typeArguments[i] = Il2CppReflectionType.From(classArguments[i]);
        }

        return Il2CppClass.FromReflectionType(MakeGenericType(Il2CppReflectionType.From(type), typeArguments));
    }

    public static Il2CppReflectionMethod* MakeGenericMethod(Il2CppReflectionMethod* method, Il2CppStructArray<Pointer<Il2CppReflectionType>> typeArguments)
    {
        var virtualMethod = method->Object.GetVirtualMethod(_makeGenericMethodMethodPointer);
        var methodPointer = (delegate* unmanaged<Il2CppReflectionMethod*, Il2CppArray*, Il2CppMethod*, Il2CppReflectionMethod*>)virtualMethod->MethodPointer;
        return methodPointer(method, typeArguments.Pointer, virtualMethod);
    }

    public static Il2CppReflectionMethod* MakeGenericMethod(Il2CppReflectionMethod* method, params Il2CppReflectionType*[] typeArguments)
    {
        var array = new Il2CppStructArray<Pointer<Il2CppReflectionType>>(typeArguments.Length);
        for (var i = 0; i < typeArguments.Length; i++)
        {
            array[i] = typeArguments[i];
        }

        return MakeGenericMethod(method, array);
    }

    public static Il2CppMethod* MakeGenericMethod(Il2CppMethod* method, params Il2CppClass*[] classArguments)
    {
        var typeArguments = new Il2CppReflectionType*[classArguments.Length];
        for (var i = 0; i < classArguments.Length; i++)
        {
            typeArguments[i] = Il2CppReflectionType.From(classArguments[i]);
        }

        return Il2CppMethod.FromReflectionMethod(MakeGenericMethod(Il2CppReflectionMethod.From(method), typeArguments));
    }
}
