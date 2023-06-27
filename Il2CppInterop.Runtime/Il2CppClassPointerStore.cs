using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Il2CppInterop.Bindings;
using Il2CppInterop.Bindings.Structs;
using Il2CppInterop.Bindings.Utilities;
using Il2CppInterop.Common;

namespace Il2CppInterop.Runtime;

public static unsafe class Il2CppClassPointerStore
{
    private static readonly Dictionary<Type, Handle<Il2CppClass>> _store = new();
    private static readonly Dictionary<Handle<Il2CppClass>, Type> _reverseStore = new();

    public static Il2CppClass* Get(Type type)
    {
        if (!_store.ContainsKey(type))
        {
            if (type.IsEnum)
            {
                if (type.IsNested)
                {
                    var declaringClass = Get(type.DeclaringType!);
                    Set(type, Il2CppRuntime.GetNestedClassFromName(declaringClass, type.Name));
                }
                else
                {
                    Set(type, Il2CppRuntime.GetClassFromName(type.Module.Name, type.Namespace, type.Name));
                }
            }
            else if (type.IsPrimitive || type == typeof(string))
            {
                RuntimeHelpers.RunClassConstructor(Type.GetType($"Il2Cpp.{type.FullName}, Il2Cpp.mscorlib", true)!.TypeHandle);
            }
            else
            {
                RuntimeHelpers.RunClassConstructor(type.TypeHandle);
            }
        }

        return _store[type];
    }

    public static Type Get(Il2CppClass* klass)
    {
        if (!_reverseStore.ContainsKey(klass))
        {
            var assemblyQualifiedName = NamePrefix.Apply(klass->Namespace) + "." + klass->Name + ", " + NamePrefix.Apply(klass->Image->Name);
            var type = Type.GetType(assemblyQualifiedName, true)!;
            Set(type, klass);
            RuntimeHelpers.RunClassConstructor(type.TypeHandle);
        }

        return _reverseStore[klass];
    }

    internal static void Set(Type type, Il2CppClass* value)
    {
        _store.Add(type, value);
        _reverseStore.Add(value, type);
    }
}

[SuppressMessage("ReSharper", "StaticMemberInGenericType")]
public static unsafe class Il2CppClassPointerStore<T>
{
    private static Il2CppClass* _pointer;

    public static Il2CppClass* Pointer
    {
        get => _pointer;
        internal set
        {
            if (_pointer == value) return;
            _pointer = value;
            Il2CppClassPointerStore.Set(typeof(T), value);
        }
    }

    static Il2CppClassPointerStore()
    {
        Pointer = Il2CppClassPointerStore.Get(typeof(T));
    }
}
