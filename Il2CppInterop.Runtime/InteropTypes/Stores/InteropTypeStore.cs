using System.Runtime.CompilerServices;
using Il2CppInterop.Bindings.Structs;
using Il2CppInterop.Bindings.Utilities;
using Il2CppInterop.Common;
using Il2CppInterop.Runtime.InteropTypes.Arrays;

namespace Il2CppInterop.Runtime.InteropTypes.Stores;

public static unsafe class InteropTypeStore
{
    private static readonly Dictionary<Pointer<Il2CppClass>, Type> _store = new();

    public static Type? Get(Il2CppClass* klass)
    {
        if (klass == null) throw new ArgumentNullException(nameof(klass));

        if (_store.TryGetValue(klass, out var value))
        {
            return value;
        }

        lock (_store)
        {
            if (!_store.ContainsKey(klass))
            {
                switch (klass->Type->Type)
                {
                    case Il2CppTypeEnum.Array:
                        var elementClass = klass->ElementClass;
                        var arrayType = elementClass->IsValueType ? typeof(Il2CppStructArray<>) : typeof(Il2CppReferenceArray<>);
                        arrayType = arrayType.MakeGenericType(Get(elementClass));
                        Set(klass, arrayType);
                        return arrayType;

                    case Il2CppTypeEnum.SzArray:
                        throw new NotImplementedException();
                }

                if (klass->IsGeneric)
                {
                    throw new NotImplementedException();
                }

                var assemblyQualifiedName = NamePrefix.Apply(klass->Namespace) + "." + klass->Name + ", " + NamePrefix.Apply(klass->Image->NameWithoutExtension);
                var type = Type.GetType(assemblyQualifiedName, true)!;
                RuntimeHelpers.RunClassConstructor(type.TypeHandle);
            }

            return _store.GetValueOrDefault(klass);
        }
    }

    internal static void Set(Il2CppClass* klass, Type interopType)
    {
        if (klass == null) throw new ArgumentNullException(nameof(klass));
        if (interopType == null) throw new ArgumentNullException(nameof(interopType));

        if (!typeof(Il2CppObjectBase).IsAssignableFrom(interopType)) throw new ArgumentException($"Interop type has to inherit from {nameof(Il2CppObjectBase)}");

        _store.Add(klass, interopType);
    }
}
