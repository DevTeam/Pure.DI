namespace Pure.DI.Core
{
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "RedundantTypeArgumentsOfMethod")]
    // ReSharper disable once ClassNeverInstantiated.Global
    internal class ResolverBuilder : IResolverBuilder
    {
        private readonly IResolverMethodsBuilder _resolverMethodsBuilder;
        private readonly ResolverMetadata _metadata;
        
        public ResolverBuilder(
            ResolverMetadata metadata,
            IResolverMethodsBuilder resolverMethodsBuilder)
        {
            _metadata = metadata;
            _resolverMethodsBuilder = resolverMethodsBuilder;
        }

        public CompilationUnitSyntax Build()
        {
            var ownerClass = (
                from cls in _metadata.SetupNode.Ancestors().OfType<ClassDeclarationSyntax>()
                where
                    cls.Modifiers.Any(i => i.Kind() == SyntaxKind.StaticKeyword)
                    && cls.Modifiers.Any(i => i.Kind() == SyntaxKind.PartialKeyword)
                    && cls.Modifiers.All(i => i.Kind() != SyntaxKind.PrivateKeyword)
                select cls).FirstOrDefault();

            var classModifiers = new List<SyntaxToken>();
            if (ownerClass == null)
            {
                classModifiers.Add(SyntaxFactory.Token(SyntaxKind.InternalKeyword));
                classModifiers.Add(SyntaxFactory.Token(SyntaxKind.StaticKeyword));
                classModifiers.Add(SyntaxFactory.Token(SyntaxKind.PartialKeyword));
                if (string.IsNullOrWhiteSpace(_metadata.TargetTypeName))
                {
                    var parentNodeName = _metadata.SetupNode
                        .Ancestors()
                        .Select(TryGetNodeName)
                        .FirstOrDefault(i => !string.IsNullOrWhiteSpace(i));
                    
                    _metadata.TargetTypeName = $"{parentNodeName}DI";
                }
            }
            else
            {
                classModifiers.AddRange(ownerClass.Modifiers);
                if (string.IsNullOrWhiteSpace(_metadata.TargetTypeName))
                {
                    _metadata.TargetTypeName = ownerClass.Identifier.Text;
                }
            }

            var compilationUnit = SyntaxFactory.CompilationUnit()
                .AddUsings(
                    SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System.Runtime.CompilerServices")));

            var originalCompilationUnit = _metadata.SetupNode.Ancestors().OfType<CompilationUnitSyntax>().FirstOrDefault();
            if (originalCompilationUnit != null)
            {
                compilationUnit = compilationUnit.AddUsings(originalCompilationUnit.Usings.ToArray());
            }

            NamespaceDeclarationSyntax? prevNamespaceNode = null;
            foreach (var originalNamespaceNode in _metadata.SetupNode.Ancestors().OfType<NamespaceDeclarationSyntax>().Reverse())
            {
                var namespaceNode = 
                    SyntaxFactory.NamespaceDeclaration(originalNamespaceNode.Name)
                        .AddUsings(originalNamespaceNode.Usings.ToArray());

                prevNamespaceNode = prevNamespaceNode == null ? namespaceNode : prevNamespaceNode.AddMembers(namespaceNode);
            }

            var resolverClass = SyntaxFactory.ClassDeclaration(_metadata.TargetTypeName)
                .AddModifiers(classModifiers.ToArray())
                .AddMembers(
                    SyntaxFactory.FieldDeclaration(
                            SyntaxFactory.VariableDeclaration(SyntaxRepo.ContextTypeSyntax)
                                .AddVariables(
                                    SyntaxFactory.VariableDeclarator(SyntaxRepo.SharedContextName)
                                        .WithInitializer(SyntaxFactory.EqualsValueClause(SyntaxFactory.ObjectCreationExpression(SyntaxRepo.ContextTypeSyntax).AddArgumentListArguments()))))
                                .AddModifiers(
                                    SyntaxFactory.Token(SyntaxKind.PrivateKeyword),
                                    SyntaxFactory.Token(SyntaxKind.StaticKeyword),
                                    SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword)))
                .AddMembers(_resolverMethodsBuilder.CreateResolveMethods().ToArray())
                .AddMembers(
                    SyntaxFactory.ClassDeclaration(SyntaxRepo.ContextClassName)
                        .AddModifiers(SyntaxFactory.Token(SyntaxKind.PrivateKeyword))
                        .AddModifiers(SyntaxFactory.Token(SyntaxKind.SealedKeyword))
                        .AddBaseListTypes(SyntaxFactory.SimpleBaseType(SyntaxRepo.IContextTypeSyntax))
                        .AddMembers(
                            SyntaxRepo.TResolveMethodSyntax
                                .AddBodyStatements(
                                    SyntaxFactory.ReturnStatement()
                                        .WithExpression(
                                            SyntaxFactory.InvocationExpression(
                                                SyntaxFactory.MemberAccessExpression(
                                                    SyntaxKind.SimpleMemberAccessExpression, 
                                                    SyntaxFactory.ParseName(_metadata.TargetTypeName), 
                                                    SyntaxFactory.Token(SyntaxKind.DotToken),
                                                    SyntaxFactory.GenericName(nameof(IContext.Resolve))
                                                        .AddTypeArgumentListArguments(SyntaxRepo.TTypeSyntax))))))
                        .AddMembers(
                            SyntaxRepo.TResolveMethodSyntax
                                .AddParameterListParameters(SyntaxFactory.Parameter(SyntaxFactory.Identifier("tag")).WithType(SyntaxRepo.ObjectTypeSyntax))
                                .AddBodyStatements(
                                    SyntaxFactory.ReturnStatement()
                                        .WithExpression(
                                            SyntaxFactory.InvocationExpression(
                                                    SyntaxFactory.MemberAccessExpression(
                                                        SyntaxKind.SimpleMemberAccessExpression, 
                                                        SyntaxFactory.ParseName(_metadata.TargetTypeName),
                                                        SyntaxFactory.Token(SyntaxKind.DotToken), 
                                                        SyntaxFactory.GenericName(nameof(IContext.Resolve))
                                                            .AddTypeArgumentListArguments(SyntaxRepo.TTypeSyntax)))
                                                .AddArgumentListArguments(SyntaxFactory.Argument(SyntaxFactory.IdentifierName("tag")))))));

            if (prevNamespaceNode != null)
            {
                prevNamespaceNode = prevNamespaceNode.AddMembers(resolverClass);
                compilationUnit = compilationUnit.AddMembers(prevNamespaceNode);
            }
            else
            {
                compilationUnit = compilationUnit.AddMembers(resolverClass);
            }

            return compilationUnit.NormalizeWhitespace();
        }

        private static string? TryGetNodeName(SyntaxNode node)
        {
            switch (node)
            {
                case ClassDeclarationSyntax classDeclaration:
                    return classDeclaration.Identifier.Text;

                case StructDeclarationSyntax structDeclaration:
                    return structDeclaration.Identifier.Text;

                case RecordDeclarationSyntax recordDeclaration:
                    return recordDeclaration.Identifier.Text;

                default:
                    return null;
            }
        }
    }
}