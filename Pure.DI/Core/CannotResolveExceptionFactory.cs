// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core
{
    using System.Linq;
    using System.Text;

    internal class CannotResolveExceptionFactory : ICannotResolveExceptionFactory
    {
        private readonly IDiagnostic _diagnostic;
        private readonly ITracer _tracer;

        public CannotResolveExceptionFactory(
            IDiagnostic diagnostic,
            ITracer tracer)
        {
            _diagnostic = diagnostic;
            _tracer = tracer;
        }

        public HandledException Create(BindingMetadata binding)
        {
            var error = new StringBuilder($"Cannot resolve {binding.Implementation?.ToString() ?? binding.Factory?.ToString() ?? binding.ToString()}, consider adding it to the DI setup. The chain of dependencies: ");
            var paths = _tracer.Paths.Where(i => i.Length > 1).ToArray();
            if (paths.Any())
            {
                var maxPath = paths.OrderBy(i => i.Length).Last();
                error.Append(": ");
                error.Append(string.Join(" -> ", maxPath.Reverse().Select(i => i.ToString())));
                error.Append(".");
            }
            else
            {
                error.Append(".");
            }
            
            var errorMessage = error.ToString();
            _diagnostic.Error(Diagnostics.Error.CannotResolve, errorMessage, binding.Implementation?.TypeSyntax.GetLocation() ?? binding.Location);
            return new HandledException(errorMessage);
        }
    }
}