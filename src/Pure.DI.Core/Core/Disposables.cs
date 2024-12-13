// ReSharper disable ArgumentsStyleStringLiteral
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable EmptyGeneralCatchClause
// ReSharper disable ArgumentsStyleLiteral

// ReSharper disable HeapView.ObjectAllocation.Evident

namespace Pure.DI.Core;

[ExcludeFromCodeCoverage]
internal static class Disposables
{
    public static readonly IDisposable Empty = EmptyDisposable.Shared;

    public static IDisposable Create(Action action) => new DisposableAction(action);

    public static IDisposable Create(params IDisposable[] disposables) =>
        Create(disposables.ToList());

    public static IDisposable Create(in IReadOnlyCollection<IDisposable> disposables) =>
        new CompositeDisposable(disposables);

    public static IDisposable Create(IEnumerable<IDisposable> disposables) =>
        new CompositeDisposable(disposables.ToImmutableArray());

    private class DisposableAction(Action action, object? key = null) : IDisposable
    {
        private readonly object? _key = key ?? action;
        private int _counter;

        public void Dispose()
        {
            if (Interlocked.Increment(ref _counter) != 1)
            {
                return;
            }

            try
            {
                action();
            }
            catch
            {
            }
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            return obj is DisposableAction other && Equals(_key, other._key);
        }

        public override int GetHashCode() =>
            _key != null ? _key.GetHashCode() : 0;
    }

    private class CompositeDisposable(in IReadOnlyCollection<IDisposable> disposables) : IDisposable
    {
        private readonly IReadOnlyCollection<IDisposable> _disposables = disposables;
        private int _counter;

        public void Dispose()
        {
            if (Interlocked.Increment(ref _counter) != 1)
            {
                return;
            }

            foreach (var disposable in _disposables)
            {
                try
                {
                    disposable.Dispose();
                }
                catch
                {
                }
            }
        }
    }

    private class EmptyDisposable : IDisposable
    {
        public static readonly IDisposable Shared = new EmptyDisposable();

        private EmptyDisposable()
        {
        }

        public void Dispose()
        {
        }
    }
}