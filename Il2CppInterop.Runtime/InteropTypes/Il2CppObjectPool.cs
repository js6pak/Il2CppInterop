using System.Runtime.Serialization;
using Il2CppInterop.Bindings.Structs;
using Il2CppInterop.Bindings.Utilities;

namespace Il2CppInterop.Runtime.InteropTypes;

public static unsafe class Il2CppObjectPool
{
    /// Il2CppObjects that are alive on il2cpp side but not necessarily on C# side (although resurrection should ensure that aswell)
    private static readonly Dictionary<Handle<Il2CppObject>, WeakReference<Il2CppObjectBase>> _alivePool = new();

    /// Cursed workaround for being able to free il2cpp gc handles and pooling at the same time
    private static readonly Dictionary<Handle<Il2CppObject>, Il2CppObjectBase> _resurrect = new();

    /// Il2CppObjects that are dead on il2cpp side and can be reused
    private static readonly Dictionary<Type, Stack<Il2CppObjectBase>> _deadPoolMap = new();

    public static Il2CppObjectBase Get(Il2CppObject* pointer)
    {
        if (_resurrect.Remove(pointer, out var resurrected))
        {
            return resurrected;
        }

        if (_alivePool.TryGetValue(pointer, out var weakReference))
        {
            if (weakReference.TryGetTarget(out var alive))
            {
                return alive;
            }

            _alivePool.Remove(pointer);
        }

        var type = Il2CppClassPointerStore.Get(pointer->Class);

        if (!_deadPoolMap.TryGetValue(type, out var deadPool) || !deadPool.TryPop(out var @object))
        {
            @object = (Il2CppObjectBase)FormatterServices.GetUninitializedObject(type);
        }

        @object.Pointer = pointer;

        return @object;
    }

    public static T Get<T>(Il2CppObject* pointer) where T : Il2CppObjectBase
    {
        return (T)Get(pointer);
    }

    internal static void Add(Il2CppObjectBase @object)
    {
        _alivePool.Add(@object.Pointer, new WeakReference<Il2CppObjectBase>(@object, true));
    }

    internal static void Resurrect(Il2CppObjectBase @object)
    {
        _resurrect.Add(@object.Pointer, @object);
    }

    // TODO hook GarbageCollector::RunFinalizer and call this
    internal static void Remove(Il2CppObject* pointer)
    {
        _resurrect.Remove(pointer);

        if (_alivePool.Remove(pointer, out var weakReference) && weakReference.TryGetTarget(out var @object))
        {
            var type = @object.GetType();

            if (!_deadPoolMap.TryGetValue(type, out var deadPool))
            {
                _deadPoolMap.Add(type, deadPool = new Stack<Il2CppObjectBase>());
            }

            deadPool.Push(@object);
        }
    }
}
