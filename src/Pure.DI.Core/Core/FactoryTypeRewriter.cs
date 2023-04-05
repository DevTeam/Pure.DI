namespace Pure.DI.Core;

internal class FactoryTypeRewriter: CSharpSyntaxRewriter, IBuilder<RewriterContext<MdFactory>, MdFactory>
{
    private readonly IMarker _marker;
    private RewriterContext<MdFactory> _context;

    public FactoryTypeRewriter(IMarker marker) => _marker = marker;

    public MdFactory Build(RewriterContext<MdFactory> context, CancellationToken cancellationToken)
    {
        _context = context;
        var factory = context.State;
        var newFactory = (SimpleLambdaExpressionSyntax)VisitSimpleLambdaExpression(factory.Factory)!;
        return factory with
        {
            Type = context.TypeConstructor.Construct(factory.SemanticModel.Compilation, factory.Type),
            Factory = newFactory,
            Resolvers = factory.Resolvers
                .Select(resolver => resolver with
                {
                    ContractType = context.TypeConstructor.Construct(factory.SemanticModel.Compilation, resolver.ContractType),
                    Tag = CreateTag(context.Injection, resolver.Tag) 
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
        
        if (injection.Tag is { } newTag)
        {
            return new MdTag(0, newTag);
        }

        return default;
    }

    public override SyntaxNode? VisitIdentifierName(IdentifierNameSyntax node)
    {
        var identifier = base.VisitIdentifierName(node) as IdentifierNameSyntax;
        if (identifier is null)
        {
            return identifier;
        }
        
        var semanticModel = _context.State.SemanticModel;
        var symbol = semanticModel.GetSymbolInfo(identifier).Symbol;
        if (symbol is not ITypeSymbol type || !_marker.IsMarkerBased(type))
        {
            return identifier;
        }
        
        var newType = _context.TypeConstructor.Construct(semanticModel.Compilation, type);
        return SyntaxFactory.IdentifierName(newType.ToDisplayString());
    }
}