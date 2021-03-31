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
        internal const string SharedContextName = "SharedContext";
        internal static readonly TypeSyntax TTypeSyntax = SyntaxFactory.ParseTypeName("T");
        internal static readonly TypeSyntax ContextTypeSyntax = SyntaxFactory.ParseTypeName("Context");
        private static readonly TypeSyntax ObjectTypeSyntax = SyntaxFactory.ParseTypeName(nameof(Object));
        private static readonly TypeSyntax ContextInterfaceTypeSyntax = SyntaxFactory.ParseTypeName(nameof(IContext));
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
            SyntaxFactory.MethodDeclaration(TTypeSyntax, nameof(IContext.Resolve))
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                .AddTypeParameterListParameters(TTypeParameterSyntax)
                .AddAttributeLists(SyntaxFactory.AttributeList().AddAttributes(AggressiveOptimizationAndInliningAttr))
                .AddConstraintClauses(SyntaxFactory.TypeParameterConstraintClause("T").AddConstraints(SyntaxFactory.ClassOrStructConstraint(SyntaxKind.ClassConstraint)));

        private static readonly MethodDeclarationSyntax StaticResolveMethodSyntax =
            ResolveMethodSyntax.AddModifiers(SyntaxFactory.Token(SyntaxKind.StaticKeyword));

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

        public CompilationUnitSyntax Build(ResolverMetadata metadata, SemanticModel semanticModel, ITypeResolver typeResolver) =>
            SyntaxFactory.CompilationUnit()
                .AddMembers(
                    SyntaxFactory.NamespaceDeclaration(SyntaxFactory.IdentifierName(metadata.Namespace))
                        .AddUsings(
                            SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(nameof(System))),
                            SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("Pure.DI")),
                            SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System.Runtime.CompilerServices")))
                        .AddMembers(
                            SyntaxFactory.ClassDeclaration(metadata.TargetTypeName)
                                .AddModifiers(
                                    SyntaxFactory.Token(SyntaxKind.InternalKeyword),
                                    SyntaxFactory.Token(SyntaxKind.StaticKeyword))
                                .AddMembers(
                                    SyntaxFactory.FieldDeclaration(
                                            SyntaxFactory.VariableDeclaration(ContextTypeSyntax)
                                                .AddVariables(
                                                    SyntaxFactory.VariableDeclarator(SharedContextName)
                                                        .WithInitializer(SyntaxFactory.EqualsValueClause(SyntaxFactory.ObjectCreationExpression(SyntaxFactory.ParseTypeName("Context")).AddArgumentListArguments()))
                                                )
                                        )
                                        .AddModifiers(
                                            SyntaxFactory.Token(SyntaxKind.PrivateKeyword),
                                            SyntaxFactory.Token(SyntaxKind.StaticKeyword),
                                            SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword)))
                                .AddMembers(
                                    CreateResolveMethods(metadata, semanticModel, typeResolver).ToArray())
                                .AddMembers(
                                    SyntaxFactory.ClassDeclaration("Context")
                                        .AddModifiers(SyntaxFactory.Token(SyntaxKind.PrivateKeyword))
                                        .AddModifiers(SyntaxFactory.Token(SyntaxKind.SealedKeyword))
                                        .AddBaseListTypes(SyntaxFactory.SimpleBaseType(ContextInterfaceTypeSyntax))
                                        .AddMembers(
                                            ResolveMethodSyntax
                                                .AddBodyStatements(
                                                    SyntaxFactory.ReturnStatement()
                                                        .WithExpression(
                                                            SyntaxFactory.InvocationExpression(
                                                                SyntaxFactory.MemberAccessExpression(
                                                                    SyntaxKind.SimpleMemberAccessExpression,
                                                                    SyntaxFactory.ParseName(metadata.TargetTypeName),
                                                                    SyntaxFactory.Token(SyntaxKind.DotToken),
                                                                    SyntaxFactory.GenericName(nameof(IContext.Resolve))
                                                                        .AddTypeArgumentListArguments(TTypeSyntax))))))
                                .AddMembers(
                                    ResolveMethodSyntax
                                        .AddParameterListParameters(SyntaxFactory.Parameter(SyntaxFactory.Identifier("tag")).WithType(ObjectTypeSyntax))
                                        .AddBodyStatements(
                                            SyntaxFactory.ReturnStatement()
                                                .WithExpression(
                                                    SyntaxFactory.InvocationExpression(
                                                        SyntaxFactory.MemberAccessExpression(
                                                            SyntaxKind.SimpleMemberAccessExpression,
                                                            SyntaxFactory.ParseName(metadata.TargetTypeName),
                                                            SyntaxFactory.Token(SyntaxKind.DotToken),
                                                            SyntaxFactory.GenericName(nameof(IContext.Resolve))
                                                                .AddTypeArgumentListArguments(TTypeSyntax)))
                                                        .AddArgumentListArguments(SyntaxFactory.Argument(SyntaxFactory.IdentifierName("tag"))))))
                                    )
                        )
                ).NormalizeWhitespace();

        private IEnumerable<MemberDeclarationSyntax> CreateResolveMethods(ResolverMetadata metadata, SemanticModel semanticModel, ITypeResolver typeResolver)
        {
            var members = new List<MemberDeclarationSyntax>();
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
                .Reverse()
                .ToList();

            var expressionStrategy = new BindingExpressionStrategy(semanticModel, typeResolver, members);

            var typeStatementsStrategy = new TypeBindingStatementsStrategy(semanticModel, expressionStrategy);
            var simpleBindings = bindings.Where(i => !i.Tags.Any()).ToList();
            members.Add(
                StaticResolveMethodSyntax.AddBodyStatements(
                        CreateResolveStatements(typeStatementsStrategy, simpleBindings, semanticModel).ToArray()));

            var typeAndTagStatementsStrategy = new TypeAndTagBindingStatementsStrategy(semanticModel, expressionStrategy);
            var tagedBindings = bindings.Where(i => i.Tags.Any()).ToList();
            members.Add(
                StaticResolveMethodSyntax
                    .AddParameterListParameters(SyntaxFactory.Parameter(SyntaxFactory.Identifier("tag")).WithType(ObjectTypeSyntax))
                    .AddBodyStatements(
                        CreateResolveStatements(typeAndTagStatementsStrategy, tagedBindings, semanticModel).ToArray()));

            return members;
        }

        private IEnumerable<StatementSyntax> CreateResolveStatements(IBindingStatementsStrategy bindingStatementsStrategy, IEnumerable<BindingMetadata> bindings, SemanticModel semanticModel)
        {
            var statements = new List<StatementSyntax>();
            foreach (var binding in bindings)
            {
                var typeExpression = SyntaxFactory.TypeOfExpression(TTypeSyntax);
                foreach (var contractType in binding.ContractTypes)
                {
                    if (!contractType.IsValidTypeToResolve(semanticModel))
                    {
                        continue;
                    }

                    var resolveStatementExpression = SyntaxFactory.IfStatement(
                        SyntaxFactory.BinaryExpression(
                            SyntaxKind.EqualsExpression,
                            SyntaxFactory.TypeOfExpression(contractType.ToTypeSyntax(semanticModel)),
                            typeExpression),
                        SyntaxFactory.Block(bindingStatementsStrategy.CreateStatements(binding, contractType))
                    );

                    statements.Add(resolveStatementExpression);
                }
            }

            statements.Add(SyntaxFactory.ReturnStatement(SyntaxFactory.DefaultExpression(TTypeSyntax)));
            return statements;
        }
    }
}