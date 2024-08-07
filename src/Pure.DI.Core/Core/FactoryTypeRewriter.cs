// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core;

internal sealed class FactoryTypeRewriter(
    IMarker marker,
    ITypeResolver typeResolver)
    : CSharpSyntaxRewriter, IBuilder<RewriterContext<MdFactory>, MdFactory>
{
    private RewriterContext<MdFactory> _context;

    public MdFactory Build(RewriterContext<MdFactory> context)
    {
        _context = context;
        var factory = context.State;
        var newFactory = (LambdaExpressionSyntax)Visit(factory.Factory)!;
        return factory with
        {
            Type = context.TypeConstructor.Construct(context.Setup, factory.SemanticModel.Compilation, factory.Type),
            Factory = newFactory,
            Resolvers = factory.Resolvers
                .Select(resolver => resolver with
                {
                    ContractType = context.TypeConstructor.Construct(context.Setup, factory.SemanticModel.Compilation, resolver.ContractType),
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

    public override SyntaxNode? Visit(SyntaxNode? node)
    {
        if (node is TypeSyntax typeSyntax && TryGetNewTypeName(typeSyntax, out var newTypeName))
        {
            return SyntaxFactory.ParseTypeName(newTypeName)
                .WithLeadingTrivia(typeSyntax.GetLeadingTrivia())
                .WithTrailingTrivia(typeSyntax.GetTrailingTrivia());
        }
        
        return base.Visit(node);
    }

    private bool TryGetNewTypeName(SyntaxNode? node, [NotNullWhen(true)] out string? newTypeName)
    {
        newTypeName = default;
        if (node is null)
        {
            return false;
        }
        
        var semanticModel = _context.State.SemanticModel;
        if (node.SyntaxTree == semanticModel.SyntaxTree)
        {
            if (semanticModel.GetSymbolInfo(node).Symbol is ITypeSymbol type)
            {
                return TryGetNewTypeName(type, true, out newTypeName);
            }
        }

        foreach (var type in semanticModel.Compilation.GetTypesByMetadataName(node.ToString()))
        {
            if (TryGetNewTypeName(type, false, out newTypeName))
            {
                return true;
            }
        }

        return false;
    }
    
    private bool TryGetNewTypeName(ITypeSymbol type, bool inTree, [NotNullWhen(true)] out string? newTypeName)
    {
        newTypeName = default;
        if (!marker.IsMarkerBased(_context.Setup, type))
        {
            return false;
        }

        var newType = _context.TypeConstructor.Construct(_context.Setup, _context.State.SemanticModel.Compilation, type);
        if (!inTree && SymbolEqualityComparer.Default.Equals(newType, type))
        {
            return false;
        }
        
        newTypeName = typeResolver.Resolve(_context.Setup, newType).Name;
        return true;
    }
}