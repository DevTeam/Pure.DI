// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core;

internal class StaticResolverNameProvider : IStaticResolverNameProvider
{
    private readonly IBuildContext _buildContext;
    private readonly IStringTools _stringTools;

    public StaticResolverNameProvider(
        IBuildContext buildContext,
        IStringTools stringTools)
    {
        _buildContext = buildContext;
        _stringTools = stringTools;
    }

    public string GetName(SemanticType dependency)
    {
        var name = GetMethodName(dependency.Type);
        return _buildContext.NameService.FindName(new MemberKey(name, dependency.Name));
    }

    private string GetMethodName(ISymbol symbol) => $"Resolve{string.Join(string.Empty, GetParts(symbol))}";

    private IEnumerable<string> GetParts(ISymbol symbol)
    {
        foreach (var part in symbol.ToDisplayParts())
        {
            // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
            switch (part.Kind)
            {
                case SymbolDisplayPartKind.NamespaceName:
                    break;

                case SymbolDisplayPartKind.Punctuation:
                    break;

                default:
                    yield return _stringTools.ConvertToTitle(part.ToString());
                    break;
            }
        }
    }
}