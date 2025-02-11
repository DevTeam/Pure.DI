#pragma warning disable CS8766 // Nullability of reference types in return type doesn't match implicitly implemented member (possibly because of nullability attributes).
namespace Pure.DI.Core;

internal sealed class SafeEnumerator<T>(IEnumerator<T> source): IEnumerator<T>
    where T: class
{
    private T? _current;
    private bool _result;

    public T? Current
    {
        get
        {
            if (!_result)
            {
                return _current;
            }
            
            _current = source.Current;
            return _current;
        }
    }

    object? IEnumerator.Current => Current;
    
    public bool MoveNext()
    {
        _result = source.MoveNext();
        return _result;
    }

    public void Reset() => source.Reset();

    public void Dispose() => source.Dispose();
}