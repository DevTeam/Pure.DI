namespace Pure.DI
{
    using System;
    using Core;
    using Microsoft.CodeAnalysis;

    [Generator]
    public class SourceGenerator : ISourceGenerator
    {
        private static readonly ISourceBuilder SourceBuilder = new SourceBuilder();

        public void Initialize(GeneratorInitializationContext context)
        {
#if DEBUG && AA
            if (!Debugger.IsAttached)
            {
                Debugger.Launch();
            }
#endif
        }

        public void Execute(GeneratorExecutionContext context)
        {
            foreach (var source in SourceBuilder.Build(context.Compilation))
            {
                try
                {
                    context.AddSource(source.HintName, source.Code);
                }
                catch (Exception ex)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        new DiagnosticDescriptor(
                            "DI-0001",
                            ex.Message,
                            ex.StackTrace,
                            "Unhandled",
                            DiagnosticSeverity.Error,
                            true),
                        source.Tree.GetRoot().GetLocation(),
                        (object)ex));
                }
            }
        }
    }
}