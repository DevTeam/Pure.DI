// ReSharper disable ClassNeverInstantiated.Global

namespace Pure.DI.Core;

using System.Runtime.CompilerServices;

// A generic factory is rewritten for every closed type it is resolved with, over the same syntax.
// Per generation, this keeps the type each name of a factory binds to and the syntax it is rewritten to.
// Its keys hold syntax nodes and semantic models, so it must not outlive the generation.
sealed class FactoryRewriteCache : IFactoryRewriteCache
{
    private readonly Dictionary<SymbolKey, ITypeSymbol?> _typeSymbols = new();
    private readonly Dictionary<TypeSyntaxKey, TypeSyntax> _typeSyntaxes = new();

    public ITypeSymbol? GetTypeSymbol(SemanticModel semanticModel, SyntaxNode node)
    {
        var key = new SymbolKey(semanticModel, node);
        if (!_typeSymbols.TryGetValue(key, out var type))
        {
            type = semanticModel.GetSymbolInfo(node).Symbol as ITypeSymbol;
            _typeSymbols.Add(key, type);
        }

        return type;
    }

    // Syntax nodes are immutable, so one node serves every rewrite of the same name to the same type.
    public TypeSyntax GetTypeSyntax(SyntaxNode node, string typeName)
    {
        var key = new TypeSyntaxKey(node, typeName);
        if (!_typeSyntaxes.TryGetValue(key, out var typeSyntax))
        {
            typeSyntax = SyntaxFactory.ParseTypeName(typeName)
                .WithLeadingTrivia(node.GetLeadingTrivia())
                .WithTrailingTrivia(node.GetTrailingTrivia());
            _typeSyntaxes.Add(key, typeSyntax);
        }

        return typeSyntax;
    }

    private readonly struct SymbolKey(SemanticModel semanticModel, SyntaxNode node) : IEquatable<SymbolKey>
    {
        private readonly SemanticModel _semanticModel = semanticModel;
        private readonly SyntaxNode _node = node;

        public bool Equals(SymbolKey other) => ReferenceEquals(_semanticModel, other._semanticModel) && ReferenceEquals(_node, other._node);

        public override bool Equals(object? obj) => obj is SymbolKey other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                return RuntimeHelpers.GetHashCode(_semanticModel) * 397 ^ RuntimeHelpers.GetHashCode(_node);
            }
        }
    }

    private readonly struct TypeSyntaxKey(SyntaxNode node, string typeName) : IEquatable<TypeSyntaxKey>
    {
        private readonly SyntaxNode _node = node;
        private readonly string _typeName = typeName;

        public bool Equals(TypeSyntaxKey other) => ReferenceEquals(_node, other._node) && _typeName == other._typeName;

        public override bool Equals(object? obj) => obj is TypeSyntaxKey other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                return RuntimeHelpers.GetHashCode(_node) * 397 ^ _typeName.GetHashCode();
            }
        }
    }
}
