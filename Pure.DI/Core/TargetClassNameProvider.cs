// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable InvertIf
namespace Pure.DI.Core
{
    using System.Linq;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal class TargetClassNameProvider : ITargetClassNameProvider
    {
        public string? TryGetName(string composerTypeName, SyntaxNode node, ClassDeclarationSyntax? ownerClass)
        {
            if (ownerClass == null)
            {
                if (string.IsNullOrWhiteSpace(composerTypeName))
                {
                    var parentNodeName =
                        node.Ancestors()
                            .Select(TryGetNodeName)
                            .FirstOrDefault(i => !string.IsNullOrWhiteSpace(i));

                    composerTypeName = $"{parentNodeName}DI";
                    return composerTypeName;
                }
            }
            else
            {
                if (string.IsNullOrWhiteSpace(composerTypeName))
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