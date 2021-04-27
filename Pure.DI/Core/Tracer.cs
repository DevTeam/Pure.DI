namespace Pure.DI.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    // ReSharper disable once ClassNeverInstantiated.Global
    internal class Tracer : ITracer, IDisposable
    {
        private readonly IDiagnostic _diagnostic;
        private readonly Stack<Dependency> _path = new();

        public Tracer(IDiagnostic diagnostic) => _diagnostic = diagnostic;

        public IDisposable RegisterResolving(Dependency dependency)
        {
            if (_path.Count <= 256)
            {
                _path.Push(dependency);
                System.Diagnostics.Debug.WriteLine(String.Join(" <- ", _path.Select(i => i.ToString())));
                return this;
            }

            var error = $"Circular dependency detected resolving {dependency}.";
            _diagnostic.Error(Diagnostics.CircularDependency, error, dependency.Binding.Location);
            throw new HandledException(error);
        }

        void IDisposable.Dispose()
        {
            if (_path.Count == 0)
            {
                System.Diagnostics.Debug.Assert(_path.Count == 0, "The path is empty.");
                return;
            }

            _path.Pop();
        }
    }
}