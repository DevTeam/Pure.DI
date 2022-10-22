// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core;

internal sealed class MemberNameService : IMemberNameService
{
    private static readonly object Id = new();
    private readonly IBuildContext _buildContext;

    public MemberNameService(IBuildContext buildContext) => _buildContext = buildContext;

    public string GetName(MemberNameKind kind)
    {
        var baseName = kind switch
        {
            MemberNameKind.ContextClass => "Context",
            MemberNameKind.ContextField => "SharedContext",
            MemberNameKind.FactoriesField => "Resolvers",
            MemberNameKind.FactoriesByTagField => "ResolversByTag",
            MemberNameKind.ResolverClass => "Resolver",
            MemberNameKind.ResolveContextClass => "ResolveContext",
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
        };

        return _buildContext.NameService.FindName(new MemberKey(baseName, Id));
    }
}