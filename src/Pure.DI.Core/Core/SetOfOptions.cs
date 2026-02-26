#pragma warning disable CS8766 // Nullability of reference types in return type doesn't match implicitly implemented member (possibly because of nullability attributes).
namespace Pure.DI.Core;

sealed class SetOfOptions<T>(IReadOnlyList<T> source)
    : ISetOfOptions<T>
{
    private bool _result;
    private T? _current;
    private int _index = -1;

    public SetOfOptions(params T[] options)
        :this((IReadOnlyList<T>)options) {}

    public bool IsStarted { get; private set; }

    public T? Current => _result ? _current : default;

    public bool MoveNext()
    {
        IsStarted = true;
        _index++;
        _result = _index < source.Count;
        _current = _result ? source[_index] : default;
        return _result;
    }

    public void Reset()
    {
        _index = -1;
        _result = false;
        _current = default;
        IsStarted = false;
    }
}
