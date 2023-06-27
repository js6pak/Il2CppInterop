using System.Collections;
using Il2CppInterop.Bindings.Structs;

namespace Il2CppInterop.Runtime.InteropTypes.Arrays;

public abstract unsafe class Il2CppArrayBase<T> : Il2CppObjectBase, IList<T>
{
    static Il2CppArrayBase()
    {
        var elementClassPointer = Il2CppClassPointerStore<T>.Pointer;
        if (elementClassPointer == default) throw new ArgumentException($"{nameof(Il2CppArrayBase<T>)} requires an Il2Cpp reference type, which {typeof(T)} isn't");
        Il2CppClassPointerStore<Il2CppArrayBase<T>>.Pointer = Il2CppArray.GetClass(elementClassPointer, 1);
    }

    private static Il2CppArray* AllocateArray(int size)
    {
        if (size < 0) throw new ArgumentOutOfRangeException(nameof(size), "Array size must not be negative");

        var classPointer = Il2CppClassPointerStore<T>.Pointer;
        if (classPointer == default) throw new ArgumentException($"{nameof(Il2CppArrayBase<T>)} requires an Il2Cpp reference type, which {typeof(T)} isn't");
        return Il2CppArray.New(classPointer, size);
    }

    protected Il2CppArrayBase(Il2CppArray* pointer) : base((Il2CppObject*)pointer)
    {
    }

    protected Il2CppArrayBase(int size) : this(AllocateArray(size))
    {
    }

    protected Il2CppArrayBase(T[] array) : this(AllocateArray(array.Length))
    {
        for (var i = 0; i < array.Length; i++)
        {
            this[i] = array[i];
        }
    }

    public new Il2CppArray* Pointer => (Il2CppArray*)base.Pointer;
    protected void* StartPointer => (byte*)Pointer + Il2CppArray.Size;
    public int Length => Pointer->Length;

    int ICollection<T>.Count => Length;
    bool ICollection<T>.IsReadOnly => true;

    public abstract T this[int index] { get; set; }

    protected void EnsureIndexIsInBounds(int index)
    {
        if (index < 0 || index >= Length) throw new IndexOutOfRangeException("Index was outside the bounds of the array.");
    }

    public int IndexOf(T item)
    {
        for (var i = 0; i < Length; i++)
        {
            if (Equals(this[i], item))
            {
                return i;
            }
        }

        return -1;
    }

    public bool Contains(T item)
    {
        return IndexOf(item) > -1;
    }

    public void CopyTo(T[] array, int arrayIndex)
    {
        if (array == null) throw new ArgumentNullException(nameof(array));
        if (arrayIndex < 0) throw new ArgumentOutOfRangeException(nameof(arrayIndex));
        if (array.Length - arrayIndex < Length) throw new ArgumentException($"Not enough space in target array: need {Length} slots, have {array.Length - arrayIndex}");

        for (var i = 0; i < Length; i++)
        {
            array[i + arrayIndex] = this[i];
        }
    }

    private static NotSupportedException FixedSizeCollectionException() => new("Collection was of a fixed size.");
    public void Clear() => throw FixedSizeCollectionException();
    public void Add(T item) => throw FixedSizeCollectionException();
    public void Insert(int index, T item) => throw FixedSizeCollectionException();
    public bool Remove(T item) => throw FixedSizeCollectionException();
    public void RemoveAt(int index) => throw FixedSizeCollectionException();

    public IEnumerator<T> GetEnumerator() => new Il2CppArrayEnumerator<T>(this);
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

internal sealed class Il2CppArrayEnumerator<T> : IEnumerator<T>
{
    private readonly Il2CppArrayBase<T> _array;
    private int _index = -1;

    internal Il2CppArrayEnumerator(Il2CppArrayBase<T> array)
    {
        _array = array;
    }

    public bool MoveNext()
    {
        _index++;

        if (_index >= _array.Length)
        {
            _index = _array.Length;
            return false;
        }

        return true;
    }

    public T Current
    {
        get
        {
            if (_index >= _array.Length)
            {
                if (_index < 0)
                {
                    throw new InvalidOperationException("Enumeration has not started. Call MoveNext.");
                }

                throw new InvalidOperationException("Enumeration already finished.");
            }

            return _array[_index];
        }
    }

    object? IEnumerator.Current => Current;

    public void Reset()
    {
        _index = -1;
    }

    public void Dispose()
    {
    }
}
