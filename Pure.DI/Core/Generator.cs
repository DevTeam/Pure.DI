// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core
{
    using System;
    using Microsoft.CodeAnalysis;

    internal class Generator : IGenerator
    {
        private readonly ISourceBuilder _sourceBuilder;
        private readonly IDiagnostic _diagnostic;
        public Generator(
            ISourceBuilder sourceBuilder,
            IDiagnostic diagnostic)
        {
            _sourceBuilder = sourceBuilder;
            _diagnostic = diagnostic;
        }

        public void Generate(GeneratorExecutionContext context)
        {
            if (_diagnostic is CompilationDiagnostic compilationDiagnostic)
            {
                compilationDiagnostic.Context = context;
            }

            try
            {
                foreach (var source in _sourceBuilder.Build(context.Compilation, context.CancellationToken))
                {
                    context.AddSource(source.HintName, source.Code);
                }
            }
            catch (HandledException)
            {
            }
            catch (Exception ex)
            {
                _diagnostic.Error(Diagnostics.Error.Unhandled, ex.ToString());
            }
        }
    }
}
