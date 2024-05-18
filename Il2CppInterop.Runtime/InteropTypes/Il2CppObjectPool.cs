using System.Collections.Concurrent;
using System.Runtime.Serialization;
using Il2CppInterop.Bindings.Structs;
using Il2CppInterop.Bindings.Utilities;
using Il2CppInterop.Runtime.InteropTypes.Stores;

namespace Il2CppInterop.Runtime.InteropTypes;

public static unsafe class Il2CppObjectPool
{
    private static readonly ConcurrentDictionary<Pointer<Il2CppObject>, WeakReference<Il2CppObjectBase>> _cache = new();
    private static readonly ConcurrentDictionary<Type, ConcurrentBag<Il2CppObjectBase>> _pools = new();

    public static Il2CppObjectBase Get(Il2CppObject* pointer)
    {
        if (pointer == null) throw new ArgumentNullException(nameof(pointer));

        if (OptionalFeatures.Il2CppObjectCaching.IsEnabled && _cache.TryGetValue(pointer, out var reference))
        {
            if (reference.TryGetTarget(out var cachedObject))
            {
                return cachedObject;
            }

            _cache.TryRemove(pointer, out _);
        }

        var type = InteropTypeStore.Get(pointer->Class)
                   ?? throw new Exception("Couldn't find interop type for " + pointer->Class->AssemblyQualifiedName);

        Il2CppObjectBase il2CppObject;

        if (OptionalFeatures.Il2CppObjectPooling.IsEnabled && _pools.TryGetValue(type, out var pool) && pool.TryTake(out var resurrectedObject))
        {
            il2CppObject = resurrectedObject;
        }
        else
        {
            il2CppObject = (Il2CppObjectBase)FormatterServices.GetUninitializedObject(type);
        }

        il2CppObject.Pointer = pointer;

        if (OptionalFeatures.Il2CppObjectCaching.IsEnabled && !_cache.TryAdd(pointer, new WeakReference<Il2CppObjectBase>(il2CppObject, true)))
        {
            throw new InvalidOperationException($"Failed to add {il2CppObject} (0x{(IntPtr)pointer:X}) to cache");
        }

        return il2CppObject;
    }

    public static T Get<T>(Il2CppObject* pointer) where T : Il2CppObjectBase
    {
        return (T)Get(pointer);
    }

    internal static void Return(Il2CppObjectBase il2CppObject)
    {
        var pool = _pools.GetOrAdd(il2CppObject.GetType(), static _ => new ConcurrentBag<Il2CppObjectBase>());
        pool.Add(il2CppObject);
    }

    // TODO hook GarbageCollector::RunFinalizer and call this
    internal static void Remove(Il2CppObject* pointer)
    {
        if (!_cache.TryRemove(pointer, out _))
        {
            throw new InvalidOperationException($"Failed to remove 0x{(IntPtr)pointer:X} from cache");
        }
    }
}
