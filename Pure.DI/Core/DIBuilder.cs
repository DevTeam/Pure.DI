namespace Pure.DI.Core
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    internal class DIBuilder
    {
        private readonly IObjectBuilder _objectBuilder;
        private static readonly TypeSyntax TTypeSyntax = SyntaxFactory.ParseTypeName("T");
        private static readonly TypeSyntax ObjectTypeSyntax = SyntaxFactory.ParseTypeName("object");
        private static readonly TypeParameterSyntax TTypeParameterSyntax = SyntaxFactory.TypeParameter("T");

        private static readonly MethodDeclarationSyntax ResolveMethodSyntax =
            SyntaxFactory.MethodDeclaration(TTypeSyntax, "Resolve")
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.StaticKeyword))
                .AddTypeParameterListParameters(TTypeParameterSyntax)
                .AddConstraintClauses(SyntaxFactory.TypeParameterConstraintClause("T").AddConstraints(SyntaxFactory.ClassOrStructConstraint(SyntaxKind.ClassConstraint)));

        public DIBuilder(IObjectBuilder objectBuilder)
        {
            _objectBuilder = objectBuilder ?? throw new ArgumentNullException(nameof(objectBuilder));
        }

        public CompilationUnitSyntax Build(DIMetadata metadata, SemanticModel semanticModel, ITypeResolver typeResolver)
        {
            return SyntaxFactory.CompilationUnit()
                .AddMembers(
                    SyntaxFactory.NamespaceDeclaration(SyntaxFactory.IdentifierName(metadata.Namespace))
                        .AddMembers(
                            SyntaxFactory.ClassDeclaration(metadata.TargetTypeName)
                                .AddModifiers(SyntaxFactory.Token(SyntaxKind.InternalKeyword))
                                .AddModifiers(SyntaxFactory.Token(SyntaxKind.StaticKeyword))
                                .AddMembers(
                                    CreateResolveMethods(metadata, semanticModel, typeResolver).ToArray()
                                )
                        )
                ).NormalizeWhitespace();
        }

        private IEnumerable<MemberDeclarationSyntax> CreateResolveMethods(DIMetadata metadata, SemanticModel semanticModel, ITypeResolver typeResolver)
        {
            var bindings = metadata.Bindings.Where(i => !i.Tags.Any()).ToList();
            if (bindings.Any())
            {
                yield return CreateGenericResolve(bindings, semanticModel, typeResolver);
            }

            var tagedbindings = metadata.Bindings.Where(i => i.Tags.Any()).ToList();
            if (tagedbindings.Any())
            {
                yield return CreateGenericResolveWithTag(tagedbindings, semanticModel, typeResolver);
            }
        }

        private MethodDeclarationSyntax CreateGenericResolve(IEnumerable<BindingMetadata> bindings, SemanticModel semanticModel, ITypeResolver typeResolver) =>
            ResolveMethodSyntax
                .AddBodyStatements(
                    bindings.SelectMany(binding => ResolveByGenericType(binding, semanticModel, typeResolver)).ToArray()
                );

        private MethodDeclarationSyntax CreateGenericResolveWithTag(IEnumerable<BindingMetadata> bindings, SemanticModel semanticModel, ITypeResolver typeResolver) =>
            ResolveMethodSyntax
                .AddParameterListParameters(SyntaxFactory.Parameter(SyntaxFactory.Identifier("tag")).WithType(ObjectTypeSyntax))
                .AddBodyStatements(
                    bindings.SelectMany(binding => ResolveByGenericTypeAndTag(binding, semanticModel, typeResolver)).ToArray()
                );

        private IEnumerable<StatementSyntax> ResolveByGenericType(BindingMetadata binding, SemanticModel semanticModel, ITypeResolver typeResolver)
        {
            var typeExpression = SyntaxFactory.TypeOfExpression(TTypeSyntax);
            foreach (var contractType in binding.ContractTypes)
            {
                ExpressionSyntax objectExpression = _objectBuilder.TryBuild(contractType, semanticModel, typeResolver);
                if (objectExpression == null)
                {
                    continue;
                }

                objectExpression = SyntaxFactory.BinaryExpression(SyntaxKind.AsExpression, objectExpression, TTypeSyntax);

                var resolveStatementExpression = SyntaxFactory.IfStatement(
                    SyntaxFactory.BinaryExpression(
                        SyntaxKind.EqualsExpression,
                        SyntaxFactory.TypeOfExpression(contractType.ToTypeSyntax(semanticModel)),
                        typeExpression),
                    SyntaxFactory.Block(
                        SyntaxFactory.ReturnStatement(objectExpression)
                    )
                );

                yield return resolveStatementExpression;
            }

            yield return SyntaxFactory.ReturnStatement(SyntaxFactory.DefaultExpression(TTypeSyntax));
        }

        private IEnumerable<StatementSyntax> ResolveByGenericTypeAndTag(BindingMetadata binding, SemanticModel semanticModel, ITypeResolver typeResolver)
        {
            var typeExpression = SyntaxFactory.TypeOfExpression(TTypeSyntax);
            foreach (var contractType in binding.ContractTypes)
            {
                ExpressionSyntax objectExpression = _objectBuilder.TryBuild(contractType, semanticModel, typeResolver);
                if (objectExpression == null)
                {
                    continue;
                }

                objectExpression = SyntaxFactory.BinaryExpression(SyntaxKind.AsExpression, objectExpression, TTypeSyntax);

                var resolveStatementExpression = SyntaxFactory.IfStatement(
                    SyntaxFactory.BinaryExpression(
                        SyntaxKind.EqualsExpression,
                        SyntaxFactory.TypeOfExpression(contractType.ToTypeSyntax(semanticModel)),
                        typeExpression),
                    SyntaxFactory.Block(
                        binding.Tags.Select(tag => (StatementSyntax)SyntaxFactory.IfStatement(
                            SyntaxFactory.InvocationExpression(
                                SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, tag, SyntaxFactory.Token(SyntaxKind.DotToken), SyntaxFactory.IdentifierName("Equals"))
                            ).AddArgumentListArguments(SyntaxFactory.Argument(SyntaxFactory.IdentifierName("tag"))),
                            SyntaxFactory.Block(SyntaxFactory.ReturnStatement(objectExpression))
                        )).ToArray()
                    )
                );

                yield return resolveStatementExpression;
            }

            yield return SyntaxFactory.ReturnStatement(SyntaxFactory.DefaultExpression(TTypeSyntax));
        }
    }
}
