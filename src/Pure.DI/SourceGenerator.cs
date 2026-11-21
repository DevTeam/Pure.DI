// ReSharper disable InvocationIsSkipped

namespace Pure.DI;

using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

[Generator(LanguageNames.CSharp)]
[SuppressMessage("MicrosoftCodeAnalysisCorrectness", "RS1036:Specify analyzer banned API enforcement setting")]
public class SourceGenerator : IIncrementalGenerator
{
    // The names of the pipeline steps, to check the incremental generation in tests.
    internal const string CompilationSnapshotStep = "Pure.DI.CompilationSnapshot";
    internal const string InterfacesStep = "Pure.DI.Interfaces";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // ReSharper disable once InvocationIsSkipped
        // Run Rider as administrator
        DebugHelper.DebugIfNeeded();
        var generator = new Generator();
        var interfaceGenerator = generator.InterfaceGenerator;

        context.RegisterPostInitializationOutput(initializationContext => {
            foreach (var apiSource in generator.Api)
            {
                initializationContext.AddSource(apiSource.HintName, apiSource.SourceText);
            }
        });

        // The compilation changes with every edit, so the snapshot is compared with the previous one
        // to skip the generation after edits that cannot change the generated code, like edits of method bodies.
        var snapshotComparer = new CompilationSnapshotComparer();
        var snapshots = context.CompilationProvider
            .Combine(context.ParseOptionsProvider)
            .Combine(context.AnalyzerConfigOptionsProvider)
            .Select(static (inputs, cancellationToken) => {
                var ((compilation, parseOptions), config) = inputs;
                return CompilationSnapshot.Create(compilation, parseOptions, config, cancellationToken);
            })
            .WithComparer(snapshotComparer)
            .WithTrackingName(CompilationSnapshotStep);

        context.RegisterSourceOutput(snapshots, (sourceProductionContext, snapshot) => {
            var hasDiagnosticsOutsideSetups = generator.Generate(
                snapshot,
                sourceProductionContext,
                sourceProductionContext.CancellationToken);

            snapshotComparer.OnGenerated(hasDiagnosticsOutsideSetups);
        });

        // Each annotated class is generated separately, and its sources are added again only when they change.
        var interfaces = context.SyntaxProvider
            .CreateSyntaxProvider(
                (node, _) => node is ClassDeclarationSyntax classSyntax
                    && (classSyntax.AttributeLists.Count > 0
                        || classSyntax.Members.Any(member => member.AttributeLists.Count > 0))
                    && interfaceGenerator.HasGenerateInterfaceAttribute(classSyntax),
                interfaceGenerator.Build)
            .WithTrackingName(InterfacesStep);

        context.RegisterSourceOutput(interfaces, interfaceGenerator.Generate);
    }
}
