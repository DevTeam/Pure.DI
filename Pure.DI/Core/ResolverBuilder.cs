namespace Pure.DI.Core
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    internal class ResolverBuilder
    {
        private readonly IObjectBuilder _objectBuilder;
        private static readonly TypeSyntax TTypeSyntax = SyntaxFactory.ParseTypeName("T");
        private static readonly TypeSyntax ObjectTypeSyntax = SyntaxFactory.ParseTypeName(nameof(Object));
        private static readonly TypeParameterSyntax TTypeParameterSyntax = SyntaxFactory.TypeParameter("T");

        private static readonly AttributeSyntax AggressiveOptimizationAndInliningAttr = SyntaxFactory.Attribute(
            SyntaxFactory.IdentifierName(nameof(MethodImplAttribute)),
            SyntaxFactory.AttributeArgumentList()
                .AddArguments(
                    SyntaxFactory.AttributeArgument(
                        SyntaxFactory.CastExpression(
                            SyntaxFactory.ParseTypeName(nameof(MethodImplOptions)),
                            SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(0x100 | 0x200))))));

        private static readonly MethodDeclarationSyntax ResolveMethodSyntax =
            SyntaxFactory.MethodDeclaration(TTypeSyntax, "Resolve")
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.StaticKeyword))
                .AddTypeParameterListParameters(TTypeParameterSyntax)
                .AddAttributeLists(
                    SyntaxFactory.AttributeList()
                        .AddAttributes(AggressiveOptimizationAndInliningAttr)
                    )
                .AddConstraintClauses(SyntaxFactory.TypeParameterConstraintClause("T").AddConstraints(SyntaxFactory.ClassOrStructConstraint(SyntaxKind.ClassConstraint)));

        private static readonly ArgumentSyntax ParamName = SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression).WithToken(SyntaxFactory.Literal("T")));
        private static readonly ArgumentSyntax ExceptionMessage = SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression).WithToken(SyntaxFactory.Literal("Cannot resolve an instance of the required type T.")));

        private static readonly StatementSyntax ThrowCannotResolveException = SyntaxFactory
            .ThrowStatement()
            .WithExpression(
                SyntaxFactory.ObjectCreationExpression(SyntaxFactory.ParseTypeName(nameof(ArgumentOutOfRangeException)))
                    .WithArgumentList(
                        SyntaxFactory.ArgumentList().AddArguments(
                            ParamName,
                            ExceptionMessage)));

        [MethodImpl((MethodImplOptions)0x100)]
        public ResolverBuilder(IObjectBuilder objectBuilder)
        {
            _objectBuilder = objectBuilder ?? throw new ArgumentNullException(nameof(objectBuilder));
        }

        public CompilationUnitSyntax Build(ResolverMetadata metadata, SemanticModel semanticModel, ITypeResolver typeResolver)
        {
            return SyntaxFactory.CompilationUnit()
                .AddMembers(
                    SyntaxFactory.NamespaceDeclaration(
                            SyntaxFactory.IdentifierName(metadata.Namespace))
                        .AddUsings(
                            SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(nameof(System))),
                            SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System.Runtime.CompilerServices")))
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

        private IEnumerable<MemberDeclarationSyntax> CreateResolveMethods(ResolverMetadata metadata, SemanticModel semanticModel, ITypeResolver typeResolver)
        {
            var bindings = (
                from binding in metadata.Bindings
                where binding.ImplementationType.IsValidTypeToResolve(semanticModel)
                let hasAnyContract = (
                    from contract in binding.ContractTypes
                    where contract.IsValidTypeToResolve(semanticModel)
                    select contract).Any()
                where hasAnyContract
                select binding
            )
                .Distinct()
                .ToList();

            // Find additional bindings
            foreach (var binding in bindings)
            {
                foreach (var contractType in binding.ContractTypes)
                {
                    _objectBuilder.TryBuild(contractType, semanticModel, typeResolver);
                }
            }

            bindings.AddRange(typeResolver.AdditionalBindings);
            bindings = bindings.Distinct().ToList();

            var simpleBindings = bindings.Where(i => !i.Tags.Any()).ToList();
            if (simpleBindings.Any())
            {
                yield return CreateGenericResolve(simpleBindings, semanticModel, typeResolver);
            }

            var tagedbindings = bindings.Where(i => i.Tags.Any()).ToList();
            if (tagedbindings.Any())
            {
                yield return CreateGenericResolveWithTag(tagedbindings, semanticModel, typeResolver);
            }
        }

        private MethodDeclarationSyntax CreateGenericResolve(IEnumerable<BindingMetadata> bindings, SemanticModel semanticModel, ITypeResolver typeResolver) =>
            ResolveMethodSyntax
                .AddBodyStatements(bindings.SelectMany(binding => ResolveGeneric(ByType, binding, semanticModel, typeResolver)).ToArray())
                .AddBodyStatements(SyntaxFactory.ReturnStatement(SyntaxFactory.DefaultExpression(TTypeSyntax)));

        private MethodDeclarationSyntax CreateGenericResolveWithTag(IEnumerable<BindingMetadata> bindings, SemanticModel semanticModel, ITypeResolver typeResolver) =>
            ResolveMethodSyntax
                .AddParameterListParameters(SyntaxFactory.Parameter(SyntaxFactory.Identifier("tag")).WithType(ObjectTypeSyntax))
                .AddBodyStatements(bindings.SelectMany(binding => ResolveGeneric(ByTypeAndTag, binding, semanticModel, typeResolver)).ToArray())
                .AddBodyStatements(SyntaxFactory.ReturnStatement(SyntaxFactory.DefaultExpression(TTypeSyntax)));

        private IEnumerable<StatementSyntax> ResolveGeneric(Func<BindingMetadata, ExpressionSyntax, IEnumerable<StatementSyntax>> resolverType, BindingMetadata binding, SemanticModel semanticModel, ITypeResolver typeResolver)
        {
            var typeExpression = SyntaxFactory.TypeOfExpression(TTypeSyntax);
            foreach (var contractType in binding.ContractTypes)
            {
                if (!contractType.IsValidTypeToResolve(semanticModel))
                {
                    continue;
                }

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
                    SyntaxFactory.Block(resolverType(binding, objectExpression))
                );

                yield return resolveStatementExpression;
            }
        }

        private static IEnumerable<StatementSyntax> ByType(BindingMetadata binding, ExpressionSyntax objectExpression)
        {
            yield return SyntaxFactory.ReturnStatement(objectExpression);
        }

        private static IEnumerable<StatementSyntax> ByTypeAndTag(BindingMetadata binding, ExpressionSyntax objectExpression) =>
            binding.Tags.Select(tag => (StatementSyntax)SyntaxFactory.IfStatement(
                    SyntaxFactory.InvocationExpression(
                        SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, tag, SyntaxFactory.Token(SyntaxKind.DotToken), SyntaxFactory.IdentifierName("Equals"))
                    ).AddArgumentListArguments(SyntaxFactory.Argument(SyntaxFactory.IdentifierName("tag"))),
                    SyntaxFactory.Block(SyntaxFactory.ReturnStatement(objectExpression))
                )
            );
    }
}