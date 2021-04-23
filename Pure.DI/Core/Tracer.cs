namespace Pure.DI.Core
{
    using System;

    // ReSharper disable once ClassNeverInstantiated.Global
    internal class Tracer : ITracer, IDisposable
    {
        private readonly IDiagnostic _diagnostic;
        private int _level;

        public Tracer(IDiagnostic diagnostic) => _diagnostic = diagnostic;

        public IDisposable RegisterResolving(Dependency dependency)
        {
            if (_level++ <= 256)
            {
                return this;
            }

            var error = $"Circular dependency detected resolving {dependency}.";
            _diagnostic.Error(Diagnostics.CircularDependency, error, dependency.Binding.Location);
            throw new HandledException(error);
        }

        void IDisposable.Dispose() => _level--;
    }
}