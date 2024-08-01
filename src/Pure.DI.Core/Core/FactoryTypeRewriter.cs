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
        var newFactory = (LambdaExpressionSyntax)Visit(factory.Factory);
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

    public override SyntaxNode? VisitIdentifierName(IdentifierNameSyntax node)
    {
        var identifier = base.VisitIdentifierName(node) as IdentifierNameSyntax;
        if (identifier is null)
        {
            return identifier;
        }
        
        var semanticModel = _context.State.SemanticModel;
        if (identifier.SyntaxTree != semanticModel.SyntaxTree)
        {
            return identifier;
        }

        var symbol = semanticModel.GetSymbolInfo(identifier).Symbol;
        if (symbol is not ITypeSymbol type)
        {
            return identifier;
        }
        
        if (!marker.IsMarkerBased(_context.Setup, type))
        {
            return identifier;
        }
        
        var newType = _context.TypeConstructor.Construct(_context.Setup, semanticModel.Compilation, type);
        var newTypeName = typeResolver.Resolve(_context.Setup, newType).Name;
        return node.WithIdentifier(
            SyntaxFactory.Identifier(newTypeName))
                .WithLeadingTrivia(node.Identifier.LeadingTrivia)
                .WithTrailingTrivia(node.Identifier.TrailingTrivia);
    }

    public override SyntaxNode? VisitTypeArgumentList(TypeArgumentListSyntax node)
    {
        var newArgs = new List<TypeSyntax>();
        var hasMarkerBased = false;
        var semanticModel = _context.Setup.SemanticModel;
        foreach (var arg in node.Arguments)
        {
            var typeName = arg.ToString();
            var isFound = false;
            foreach (var type in semanticModel.Compilation.GetTypesByMetadataName(typeName))
            {
                if (!marker.IsMarkerBased(_context.Setup, type))
                {
                    newArgs.Add(arg);
                    isFound = true;
                    break;
                }

                hasMarkerBased = true;
                var constructedType = _context.TypeConstructor.Construct(_context.Setup, semanticModel.Compilation, type);
                if (SymbolEqualityComparer.Default.Equals(type, constructedType))
                {
                    continue;
                }
                
                newArgs.Add(SyntaxFactory.ParseTypeName(constructedType.ToString()));
                isFound = true;
                break;
            }

            if (!isFound)
            {
                return base.VisitTypeArgumentList(node);
            }
        }

        return hasMarkerBased
            ? SyntaxFactory.TypeArgumentList().AddArguments(newArgs.ToArray())
            : base.VisitTypeArgumentList(node);
    }
}