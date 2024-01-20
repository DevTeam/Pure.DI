// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core;

internal sealed class FactoryTypeRewriter(IMarker marker)
    : CSharpSyntaxRewriter, IBuilder<RewriterContext<MdFactory>, MdFactory>
{
    private RewriterContext<MdFactory> _context;

    public MdFactory Build(RewriterContext<MdFactory> context)
    {
        _context = context;
        var factory = context.State;
        var newFactory = (LambdaExpressionSyntax)Visit(factory.Factory);
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
        if (symbol is not ITypeSymbol type || !marker.IsMarkerBased(type))
        {
            return identifier;
        }
        
        var newType = _context.TypeConstructor.Construct(semanticModel.Compilation, type);
        return node.WithIdentifier(
            SyntaxFactory.Identifier(newType.ToString())
                .WithLeadingTrivia(node.Identifier.LeadingTrivia)
                .WithTrailingTrivia(node.Identifier.TrailingTrivia));
    }
}