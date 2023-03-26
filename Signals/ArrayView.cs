using System.Collections;

namespace Meander.Signals;

public readonly struct ArrayView<T>
{
    private readonly T[] _array;
    private readonly int _start, _end;

    public ArrayView(T[] array, int index, int count)
    {
        _array = array;
        _start = index;
        _end = index + count;
    }

    public Span<T> Values => _array.AsSpan(_start.._end);
}
