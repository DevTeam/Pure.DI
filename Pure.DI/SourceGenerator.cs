namespace Pure.DI
{
    using System.Diagnostics;
    using System.Text;
    using Core;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.Text;

    [Generator]
    public class SourceGenerator: ISourceGenerator
    {
        private static readonly ResolverBuilder Builder = new ResolverBuilder(new ObjectBuilder(new ConstructorsResolver()));

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
            foreach (var tree in context.Compilation.SyntaxTrees)
            {
                var semanticModel = context.Compilation.GetSemanticModel(tree);
                var walker = new MetadataWalker(semanticModel);
                walker.Visit(tree.GetRoot());
                if (walker.Metadata.Count > 0)
                {
                    foreach (var metadata in walker.Metadata)
                    {
                        var typeResolver = new TypeResolver(metadata, semanticModel);
                        var compilationUnitSyntax = Builder.Build(metadata, semanticModel, typeResolver);
                        context.AddSource(metadata.TargetTypeName, SourceText.From(compilationUnitSyntax.ToString(), Encoding.UTF8));
                    }
                }
            }
        }
    }
}