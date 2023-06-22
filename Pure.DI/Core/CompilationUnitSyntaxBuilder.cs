// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable LoopCanBeConvertedToQuery
namespace Pure.DI.Core;

#if ROSLYN38
using NamespaceType = Microsoft.CodeAnalysis.CSharp.Syntax.NamespaceDeclarationSyntax;
#else
using NamespaceType = BaseNamespaceDeclarationSyntax;
#endif

internal sealed class CompilationUnitSyntaxBuilder : ICompilationUnitSyntaxBuilder
{
    private readonly ResolverMetadata _metadata;

    public CompilationUnitSyntaxBuilder(ResolverMetadata metadata) =>
        _metadata = metadata;

    public CompilationUnitSyntax CreateRootNode(ClassDeclarationSyntax resolverClass) =>
        CreateRootNode(
            _metadata.SetupNode,
            new []{ SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(Defaults.DefaultNamespace).WithSpace()) },
            resolverClass);
        
    [Pure]
    private static CompilationUnitSyntax CreateRootNode(SyntaxNode targetNode, UsingDirectiveSyntax[] additionalUsingSet, params MemberDeclarationSyntax[] members)
    {
        var namespaces = targetNode.Ancestors().OfType<NamespaceType>();
        NamespaceType? rootNamespace = default;
        foreach (var ns in namespaces)
        {
            var nextNs = ns.WithMembers(new SyntaxList<MemberDeclarationSyntax>(Enumerable.Empty<MemberDeclarationSyntax>()));
            rootNamespace = rootNamespace == default
                ? nextNs.AddMembers(members).AddUsings(GetUsingSet(nextNs.Usings, additionalUsingSet))
                : nextNs.AddMembers(rootNamespace);
        }

        var baseCompilationUnit = targetNode.Ancestors().OfType<CompilationUnitSyntax>().FirstOrDefault();
        var rootCompilationUnit = (baseCompilationUnit ?? SyntaxFactory.CompilationUnit())
            .WithMembers(new SyntaxList<MemberDeclarationSyntax>(Enumerable.Empty<MemberDeclarationSyntax>()));

        return rootNamespace != default
            ? rootCompilationUnit.AddMembers(rootNamespace)
            : rootCompilationUnit.AddUsings(GetUsingSet(rootCompilationUnit.Usings, additionalUsingSet)).AddMembers(members);
    }

    [Pure]
    private static UsingDirectiveSyntax[] GetUsingSet(IEnumerable<UsingDirectiveSyntax> usingSet, IEnumerable<UsingDirectiveSyntax> additionalUsingSet)
    {
        var currentUsingSet = usingSet.Select(i => i.Name?.ToString() ?? string.Empty).ToImmutableHashSet();
        return additionalUsingSet.Where(i => !currentUsingSet.Contains(i.Name?.ToString() ?? string.Empty)).ToArray();
    }
}