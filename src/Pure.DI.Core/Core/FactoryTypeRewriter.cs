// ReSharper disable ClassNeverInstantiated.Global

namespace Pure.DI.Core;

sealed class FactoryTypeRewriter(
    IMarker marker,
    ITypeResolver typeResolver,
    ITypes types)
    : CSharpSyntaxRewriter, IFastBuilder<RewriterContext<MdFactory>, MdFactory>
{
    private RewriterContext<MdFactory> _context;

    public MdFactory Build(in RewriterContext<MdFactory> context)
    {
        _context = context;
        var factory = context.State;
        var newFactory = (LambdaExpressionSyntax)Visit(factory.Factory);
        return factory with
        {
            Type = context.TypeConstructor.Construct(context.Setup, factory.Type),
            Factory = newFactory,
            Resolvers = factory.Resolvers
                .Select(resolver => resolver with
                {
                    ContractType = _context.TypeConstructor.Construct(_context.Setup, resolver.ContractType),
                    Tag = CreateTag(_context.Injection, resolver.Tag)
                })
                .ToImmutableArray()
        };
    }

    private static MdTag? CreateTag(in Injection injection, in MdTag? tag)
    {
        if (!tag.HasValue || !ReferenceEquals(tag.Value.Value, MdTag.ContextTag))
        {
            return tag;
        }

        if (injection.Tag is {} newTag)
        {
            return new MdTag(0, newTag);
        }

        return null;
    }

    public override SyntaxNode? VisitIdentifierName(IdentifierNameSyntax node) =>
        TryCreateTypeSyntax(node) ?? base.VisitIdentifierName(node);

    public override SyntaxNode? VisitQualifiedName(QualifiedNameSyntax node) =>
        TryCreateTypeSyntax(node) ?? base.VisitQualifiedName(node);

    private SyntaxNode? TryCreateTypeSyntax(SyntaxNode node) =>
        TryGetNewTypeName(node, !IsObjectCreationType(node), out var newTypeName)
            ? SyntaxFactory.ParseTypeName(newTypeName)
                .WithLeadingTrivia(node.GetLeadingTrivia())
                .WithTrailingTrivia(node.GetTrailingTrivia())
            : null;

    private bool TryGetNewTypeName(SyntaxNode? node, bool includeNullableAnnotation, [NotNullWhen(true)] out string? newTypeName)
    {
        newTypeName = null;
        if (node is null)
        {
            return false;
        }

        var semanticModel = _context.State.SemanticModel;
        if (node.SyntaxTree == semanticModel.SyntaxTree)
        {
            if (semanticModel.GetSymbolInfo(node).Symbol is ITypeSymbol type)
            {
                return TryGetNewTypeName(type, true, includeNullableAnnotation, out newTypeName);
            }
        }
        else
        {
            if (semanticModel.Compilation.GetTypeByMetadataName(node.ToString()) is {} parsedType)
            {
                return TryGetNewTypeName(parsedType, true, includeNullableAnnotation, out newTypeName);
            }
        }

        return false;
    }

    private bool TryGetNewTypeName(ITypeSymbol type, bool inTree, bool includeNullableAnnotation, [NotNullWhen(true)] out string? newTypeName)
    {
        newTypeName = null;
        if (!marker.IsMarkerBased(_context.Setup, type))
        {
            return false;
        }

        var newType = _context.TypeConstructor.Construct(_context.Setup, type);
        if (!inTree && types.TypeEquals(newType, type))
        {
            return false;
        }

        if (!includeNullableAnnotation)
        {
            newType = RemoveNullableAnnotation(newType);
        }

        newTypeName = typeResolver.Resolve(_context.Setup, newType).Name;
        return true;
    }

    private static bool IsObjectCreationType(SyntaxNode node) =>
        node.Parent is ObjectCreationExpressionSyntax objectCreation && objectCreation.Type == node;

    private static ITypeSymbol RemoveNullableAnnotation(ITypeSymbol type) =>
        type switch
        {
            INamedTypeSymbol namedType => namedType.WithNullableAnnotation(NullableAnnotation.NotAnnotated),
            IArrayTypeSymbol arrayType => arrayType.WithNullableAnnotation(NullableAnnotation.NotAnnotated),
            _ => type.WithNullableAnnotation(NullableAnnotation.NotAnnotated)
        };
}
