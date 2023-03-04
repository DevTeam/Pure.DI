namespace Pure.DI.Core.Models;

internal interface IMdAttribute
{
    SemanticModel SemanticModel { get; }
    
    SyntaxNode Source { get; }

    ITypeSymbol AttributeType { get; }
    
    int ArgumentPosition { get; }
}