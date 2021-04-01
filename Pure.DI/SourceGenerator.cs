namespace Pure.DI
{
    using System;
    using System.Text;
    using Core;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.Text;

    [Generator]
    public class SourceGenerator: ISourceGenerator
    {
        private static readonly IConstructorsResolver ConstructorsResolver = new ConstructorsResolver();
        private static readonly IObjectBuilder ConstructorObjectBuilder = new ConstructorObjectBuilder(ConstructorsResolver);
        private static readonly IObjectBuilder FactoryObjectBuilder = new FactoryObjectBuilder();

        public void Initialize(GeneratorInitializationContext context)
        {
#if DEBUG && A
            if (!Debugger.IsAttached)
            {
                Debugger.Launch();
            }
#endif
        }

        public void Execute(GeneratorExecutionContext context)
        {
            var builder = new ResolverBuilder();
            foreach (var tree in context.Compilation.SyntaxTrees)
            {
                try
                {
                    var semanticModel = context.Compilation.GetSemanticModel(tree);
                    var walker = new MetadataWalker(semanticModel);
                    walker.Visit(tree.GetRoot());
                    if (walker.Metadata.Count > 0)
                    {
                        foreach (var metadata in walker.Metadata)
                        {
                            var typeResolver = new TypeResolver(metadata, semanticModel, ConstructorObjectBuilder, FactoryObjectBuilder);
                            var compilationUnitSyntax = builder.Build(metadata, semanticModel, typeResolver);
                            context.AddSource(metadata.TargetTypeName, SourceText.From(compilationUnitSyntax.ToString(), Encoding.UTF8));
                        }
                    }
                }
                catch (Exception ex)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        new DiagnosticDescriptor(
                            "SG0001",
                            ex.Message,
                            "Method {0} returns. All methods must return void.",
                            "yeet",
                            DiagnosticSeverity.Error,
                            true), tree.GetRoot().GetLocation(), (object)ex));
                }
            }

        }
    }
}