namespace Pure.DI.Core
{
    using System;

    // ReSharper disable once ClassNeverInstantiated.Global
    internal class Tracer : ITracer, IDisposable
    {
        private readonly IDiagnostic _diagnostic;
        private int _level;

        public Tracer(IDiagnostic diagnostic) => _diagnostic = diagnostic;

        public IDisposable RegisterResolving(TypeResolveDescription typeResolveDescription)
        {
            if (_level++ <= 256)
            {
                return this;
            }

            var message = $"Circular dependency detected resolving {typeResolveDescription}.";
            _diagnostic.Error(Diagnostics.CircularDependency, message, typeResolveDescription.Binding.Location);
            return this;
        }

        void IDisposable.Dispose() => _level--;
    }
}