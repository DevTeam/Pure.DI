// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core
{
    using System.Linq;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal class TargetClassNameProvider : ITargetClassNameProvider
    {
        private readonly IDiagnostic _diagnostic;

        public TargetClassNameProvider(IDiagnostic diagnostic) =>
            _diagnostic = diagnostic;

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
                    _diagnostic.Information(Diagnostics.CannotUseCurrentType, $"It is not possible to use the current type as DI. Please make sure it is static partial and has public or internal access modifiers. {targetTypeName} will be used instead. You may change this name by passing the optional argument to DI.Setup(string targetTypeName).", node.GetLocation());
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