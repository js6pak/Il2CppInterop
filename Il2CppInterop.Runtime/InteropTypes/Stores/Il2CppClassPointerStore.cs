using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Il2CppInterop.Bindings;
using Il2CppInterop.Bindings.Structs;
using Il2CppInterop.Bindings.Utilities;

namespace Il2CppInterop.Runtime.InteropTypes.Stores;

public static unsafe class Il2CppClassPointerStore
{
    private static readonly Dictionary<Type, Pointer<Il2CppClass>> _store = new();

    private static bool TryFind(Type type, out Il2CppClass* klass)
    {
        if (type.IsPointer)
        {
            var pointerElementType = type.GetElementType()!;
            klass = Il2CppClass.MakePointerClass(Get(pointerElementType));
            return true;
        }

        if (type.IsConstructedGenericType && type.GetGenericTypeDefinition() == typeof(Pointer<>))
        {
            var pointerElementType = type.GenericTypeArguments.Single();
            klass = Il2CppClass.MakePointerClass(Get(pointerElementType));
            return true;
        }

        if (type.IsEnum)
        {
            if (type.IsNested)
            {
                var declaringClass = Get(type.DeclaringType!);
                klass = Il2CppRuntime.GetNestedClassFromName(declaringClass, type.Name);
            }
            else
            {
                klass = Il2CppRuntime.GetClassFromName(type.Module.Name, type.Namespace, type.Name);
            }

            if (klass == null)
            {
                throw new InvalidOperationException("Couldn't find the il2cpp class for " + type.AssemblyQualifiedName);
            }

            return true;
        }

        if (type.IsPrimitive || type == typeof(string))
        {
            try
            {
                var interopType = Type.GetType($"Il2Cpp.{type.FullName}, Il2Cpp.mscorlib")
                                  ?? throw new TypeLoadException("Failed to find interop type for " + type);
                klass = Get(interopType);
                return true;
            }
            catch (Exception e)
            {
                throw new Exception("Failed to load interop type for " + type, e);
            }
        }

        klass = null;
        return false;
    }

    public static Il2CppClass* Get(Type type)
    {
        if (type == null) throw new ArgumentNullException(nameof(type));


        if (_store.TryGetValue(type, out var value))
        {
            return value;
        }

        lock (_store)
        {
            if (!_store.ContainsKey(type))
            {
                if (TryFind(type, out var klass))
                {
                    Set(type, klass);
                    return klass;
                }

                RuntimeHelpers.RunClassConstructor(type.TypeHandle);
            }

            return _store[type];
        }
    }

    internal static void Set(Type type, Il2CppClass* value)
    {
        if (type == null) throw new ArgumentNullException(nameof(type));
        if (value == null) throw new ArgumentNullException(nameof(value));

        _store.Add(type, value);
    }
}

[SuppressMessage("ReSharper", "StaticMemberInGenericType")]
public static unsafe class Il2CppClassPointerStore<T>
{
    private static Il2CppClass* _pointer;
    private static bool _initialized;

    public static Il2CppClass* Pointer
    {
        get
        {
            if (!_initialized)
            {
                _pointer = Il2CppClassPointerStore.Get(typeof(T));
                _initialized = true;
            }

            return _pointer;
        }
        internal set
        {
            if (_pointer == value) return;

            Il2CppClassPointerStore.Set(typeof(T), value);
            _pointer = value;
            _initialized = true;
        }
    }
}
