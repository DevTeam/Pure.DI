namespace Pure.DI.Core;

using NS35EBD81B;

// ReSharper disable once ClassNeverInstantiated.Global
internal class StaticResolveMethodBuilder : IResolveMethodBuilder
{
    private readonly ISyntaxRegistry _syntaxRegistry;
    private readonly ISettings _settings;

    public StaticResolveMethodBuilder(
        ISyntaxRegistry syntaxRegistry,
        ISettings settings)
    {
        _syntaxRegistry = syntaxRegistry;
        _settings = settings;
    }

    public ResolveMethod Build() =>
        new(SyntaxRepo.CreateStaticResolveMethodSyntax(_settings.AccessibilityToken).AddBodyStatements(_syntaxRegistry.FindMethod(nameof(ResolversTable), nameof(ResolversTable.Resolve)).Body!.Statements.ToArray()));
}