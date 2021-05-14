// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core
{
    using System.Linq;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal class TargetClassNameProvider : ITargetClassNameProvider
    {
        public string? TryGetName(string targetTypeName, SyntaxNode node, ClassDeclarationSyntax? ownerClass)
        {
            if (ownerClass == null)
            {
                if (string.IsNullOrWhiteSpace(targetTypeName))
                {
                    var parentNodeName =
                        node.Ancestors()
                            .Select(TryGetNodeName)
                            .FirstOrDefault(i => !string.IsNullOrWhiteSpace(i));

                    targetTypeName = $"{parentNodeName}DI";
                    return targetTypeName;
                }
            }
            else
            {
                if (string.IsNullOrWhiteSpace(targetTypeName))
                {
                    return ownerClass.Identifier.Text;
                }
            }

            return null;
        }

        private static string? TryGetNodeName(SyntaxNode node) =>
            node switch
            {
                ClassDeclarationSyntax classDeclaration => classDeclaration.Identifier.Text,
                StructDeclarationSyntax structDeclaration => structDeclaration.Identifier.Text,
                RecordDeclarationSyntax recordDeclaration => recordDeclaration.Identifier.Text,
                _ => null
            };
    }
}