// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core;

internal class SyntaxFilter : ISyntaxFilter
{
    private static readonly Type[] Types =
    {
        typeof(BaseListSyntax), typeof(ArgumentListSyntax), typeof(TypeArgumentListSyntax), typeof(AttributeListSyntax), typeof(ConstructorDeclarationSyntax), typeof(InvocationExpressionSyntax)
    };

    private static readonly Type[] TypesWithAttribute =
    {
        typeof(BaseMethodDeclarationSyntax), typeof(BasePropertyDeclarationSyntax), typeof(BaseFieldDeclarationSyntax)
    };

    public IComparable Order => 0;

    public bool Accept(SyntaxNode node)
    {
        // ReSharper disable once ConvertIfStatementToReturnStatement
        if (Types.Any(i => i.IsInstanceOfType(node)))
        {
            return true;
        }

        return TypesWithAttribute.Any(i => i.IsInstanceOfType(node));
    }
}