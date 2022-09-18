using System;
using System.Runtime.InteropServices;
using Il2CppInterop.Runtime.Runtime;

namespace Il2CppInterop.Runtime.Injection;

public unsafe partial class ClassInjector
{
    public static void Dump<T>()
    {
        Dump((Il2CppClass*)Il2CppClassPointerStore<T>.NativeClassPtr);
    }

    private static string ToString(Il2CppClass* il2CppClass)
    {
        if (il2CppClass == default) return "null";
        var classStruct = UnityVersionHandler.Wrap(il2CppClass);
        return $"{Marshal.PtrToStringUTF8(classStruct.Namespace)}.{Marshal.PtrToStringUTF8(classStruct.Name)}";
    }

    private static string ToString(Il2CppTypeStruct* il2CppType)
    {
        if (il2CppType == default) return "null";
        return Marshal.PtrToStringAnsi(IL2CPP.il2cpp_type_get_name((IntPtr)il2CppType));
    }

    public static void Dump(Il2CppClass* il2CppClass)
    {
        InjectorHelpers.Setup();
        InjectorHelpers.ClassInit(il2CppClass);

        var classStruct = UnityVersionHandler.Wrap(il2CppClass);

        Console.WriteLine("Dumping " + classStruct.Pointer);

        Console.WriteLine($" Namespace = {Marshal.PtrToStringUTF8(classStruct.Namespace)}");
        Console.WriteLine($" Name = {Marshal.PtrToStringUTF8(classStruct.Name)}");

        Console.WriteLine($" Parent = {ToString(classStruct.Parent)}");
        Console.WriteLine($" InstanceSize = {classStruct.InstanceSize}");
        Console.WriteLine($" NativeSize = {classStruct.NativeSize}");
        Console.WriteLine($" ActualSize = {classStruct.ActualSize}");
        Console.WriteLine($" Flags = {classStruct.Flags}");
        Console.WriteLine($" ValueType = {classStruct.ValueType}");
        Console.WriteLine($" EnumType = {classStruct.EnumType}");
        Console.WriteLine($" IsGeneric = {classStruct.IsGeneric}");
        Console.WriteLine($" Initialized = {classStruct.Initialized}");
        Console.WriteLine($" InitializedAndNoError = {classStruct.InitializedAndNoError}");
        Console.WriteLine($" SizeInited = {classStruct.SizeInited}");
        Console.WriteLine($" HasFinalize = {classStruct.HasFinalize}");
        Console.WriteLine($" IsVtableInitialized = {classStruct.IsVtableInitialized}");

        var vtable = (VirtualInvokeData*)classStruct.VTable;
        Console.WriteLine($" VTable ({classStruct.VtableCount}):");
        for (var i = 0; i < classStruct.VtableCount; i++)
        {
            var virtualInvokeData = vtable![i];
            var methodName = virtualInvokeData.method == default ? "<null>" : Marshal.PtrToStringUTF8(UnityVersionHandler.Wrap(virtualInvokeData.method).Name);

            Console.WriteLine($"  [{i}] {methodName} - {(virtualInvokeData.methodPtr == default ? "<null>" : virtualInvokeData.methodPtr)}");
        }

        Console.WriteLine($" Fields ({classStruct.FieldCount}):");
        for (var i = 0; i < classStruct.FieldCount; i++)
        {
            var field = UnityVersionHandler.Wrap(classStruct.Fields + i * UnityVersionHandler.FieldInfoSize());

            Console.WriteLine($"  [{i}] {ToString(field.Type)} {Marshal.PtrToStringUTF8(field.Name)} - {field.Offset}");
        }

        Console.WriteLine($" Methods ({classStruct.MethodCount}):");
        for (var i = 0; i < classStruct.MethodCount; i++)
        {
            var method = UnityVersionHandler.Wrap(classStruct.Methods[i]);

            Console.WriteLine($"  [{i}] {ToString(method.ReturnType)} {Marshal.PtrToStringUTF8(method.Name)}({method.ParametersCount}), {method.Flags & Il2CppMethodFlags.METHOD_ATTRIBUTE_VTABLE_LAYOUT_MASK}, {method.Flags}, {method.Slot}");
        }

        Console.WriteLine($" ImplementedInterfaces ({classStruct.InterfaceCount}):");
        for (var i = 0; i < classStruct.InterfaceCount; i++)
        {
            var @interface = UnityVersionHandler.Wrap(classStruct.ImplementedInterfaces[i]);

            Console.WriteLine($"  [{i}] {Marshal.PtrToStringUTF8(@interface.Name)}");
        }

        Console.WriteLine($" InterfaceOffsets ({classStruct.InterfaceOffsetsCount}):");
        for (var i = 0; i < classStruct.InterfaceOffsetsCount; i++)
        {
            var pair = classStruct.InterfaceOffsets[i];
            var @interface = UnityVersionHandler.Wrap(pair.interfaceType);

            Console.WriteLine($"  [{i}] {pair.offset} - {Marshal.PtrToStringUTF8(@interface.Name)}");
        }

        Console.WriteLine($" TypeHierarchy ({classStruct.TypeHierarchyDepth}):");
        for (var i = 0; i < classStruct.TypeHierarchyDepth; i++)
        {
            var @interface = UnityVersionHandler.Wrap(classStruct.TypeHierarchy[i]);

            Console.WriteLine($"  [{i}] {Marshal.PtrToStringUTF8(@interface.Name)}");
        }
    }
}
