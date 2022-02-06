namespace Pure.DI.Core;

using NS35EBD81B;

// ReSharper disable once ClassNeverInstantiated.Global
internal class StaticResolveMethodBuilder : IResolveMethodBuilder
{
    private readonly ISyntaxRegistry _syntaxRegistry;

    public StaticResolveMethodBuilder(ISyntaxRegistry syntaxRegistry) =>
        _syntaxRegistry = syntaxRegistry;

    public ResolveMethod Build() =>
        new(SyntaxRepo.StaticResolveMethodSyntax.AddBodyStatements(_syntaxRegistry.FindMethod(nameof(ResolversTable), nameof(ResolversTable.Resolve)).Body!.Statements.ToArray()));
}