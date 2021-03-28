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
        private static readonly DIBuilder Builder = new DIBuilder(new ObjectBuilder(new ConstructorsResolver()));

        public void Initialize(GeneratorInitializationContext context)
        {
#if DEBUG && AA
            if (!Debugger.IsAttached)
            {
                Debugger.Launch();
            }
#endif
            context.RegisterForSyntaxNotifications(() => new MetadataReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            if (
                context.SyntaxContextReceiver is MetadataReceiver bindingsReceiver
                && bindingsReceiver.Metadata.Count > 0)
            {
                foreach (var metadata in bindingsReceiver.Metadata)
                {
                    var semanticModel = context.Compilation.GetSemanticModel(metadata.Tree);
                    var typeResolver = new TypeResolver(metadata);
                    var compilationUnitSyntax = Builder.Build(metadata, semanticModel, typeResolver);
                    context.AddSource(metadata.TargetTypeName, SourceText.From(compilationUnitSyntax.ToString(), Encoding.UTF8));
                }
            }
        }
    }
}