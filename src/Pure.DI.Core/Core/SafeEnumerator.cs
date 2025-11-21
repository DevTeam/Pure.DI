#pragma warning disable CS8766 // Nullability of reference types in return type doesn't match implicitly implemented member (possibly because of nullability attributes).
namespace Pure.DI.Core;

sealed class SafeEnumerator<T>(IEnumerator<T> source) : IEnumerator<T>
    where T : class
{
    private bool _result;

    public T? Current
    {
        get
        {
            if (!_result)
            {
                return field;
            }

            field = source.Current;
            return field;
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