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
        private const string ContextClassName = "Context";
        internal static readonly TypeSyntax TTypeSyntax = SyntaxFactory.ParseTypeName("T");
        internal static readonly TypeSyntax ContextTypeSyntax = SyntaxFactory.ParseTypeName(ContextClassName);
        internal static readonly TypeSyntax ObjectTypeSyntax = SyntaxFactory.ParseTypeName(nameof(Object));
        private static readonly TypeSyntax TypeTypeSyntax = SyntaxFactory.ParseTypeName(nameof(Type));
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

        private static readonly MethodDeclarationSyntax TResolveMethodSyntax =
            SyntaxFactory.MethodDeclaration(TTypeSyntax, nameof(IContext.Resolve))
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                .AddAttributeLists(SyntaxFactory.AttributeList().AddAttributes(AggressiveOptimizationAndInliningAttr))
                .AddTypeParameterListParameters(TTypeParameterSyntax);

        private static readonly MethodDeclarationSyntax GenericStaticResolveMethodSyntax =
            TResolveMethodSyntax.AddModifiers(SyntaxFactory.Token(SyntaxKind.StaticKeyword));

        private static readonly MethodDeclarationSyntax GenericStaticResolveWithTagMethodSyntax =
            GenericStaticResolveMethodSyntax.AddParameterListParameters(SyntaxFactory.Parameter(SyntaxFactory.Identifier("tag")).WithType(ObjectTypeSyntax));

        private static readonly MethodDeclarationSyntax ObjectResolveMethodSyntax =
            SyntaxFactory.MethodDeclaration(ObjectTypeSyntax, nameof(IContext.Resolve))
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                .AddAttributeLists(SyntaxFactory.AttributeList().AddAttributes(AggressiveOptimizationAndInliningAttr))
                .AddParameterListParameters(SyntaxFactory.Parameter(SyntaxFactory.Identifier("type")).WithType(TypeTypeSyntax));

        private static readonly MethodDeclarationSyntax StaticResolveMethodSyntax =
            ObjectResolveMethodSyntax.AddModifiers(SyntaxFactory.Token(SyntaxKind.StaticKeyword));

        private static readonly MethodDeclarationSyntax StaticResolveWithTagMethodSyntax =
            StaticResolveMethodSyntax.AddParameterListParameters(SyntaxFactory.Parameter(SyntaxFactory.Identifier("tag")).WithType(ObjectTypeSyntax));

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
                                    SyntaxFactory.Token(SyntaxKind.StaticKeyword),
                                    SyntaxFactory.Token(SyntaxKind.PartialKeyword))
                                .AddMembers(
                                    SyntaxFactory.FieldDeclaration(
                                            SyntaxFactory.VariableDeclaration(ContextTypeSyntax)
                                                .AddVariables(
                                                    SyntaxFactory.VariableDeclarator(SharedContextName)
                                                        .WithInitializer(SyntaxFactory.EqualsValueClause(SyntaxFactory.ObjectCreationExpression(ContextTypeSyntax).AddArgumentListArguments()))
                                                )
                                        )
                                        .AddModifiers(
                                            SyntaxFactory.Token(SyntaxKind.PrivateKeyword),
                                            SyntaxFactory.Token(SyntaxKind.StaticKeyword),
                                            SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword)))
                                .AddMembers(
                                    CreateResolveMethods(metadata, semanticModel, typeResolver).ToArray())
                                .AddMembers(
                                    SyntaxFactory.ClassDeclaration(ContextClassName)
                                        .AddModifiers(SyntaxFactory.Token(SyntaxKind.PrivateKeyword))
                                        .AddModifiers(SyntaxFactory.Token(SyntaxKind.SealedKeyword))
                                        .AddBaseListTypes(SyntaxFactory.SimpleBaseType(ContextInterfaceTypeSyntax))
                                        .AddMembers(
                                            TResolveMethodSyntax
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
                                            TResolveMethodSyntax
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

        private static IEnumerable<MemberDeclarationSyntax> CreateResolveMethods(ResolverMetadata metadata, SemanticModel semanticModel, ITypeResolver typeResolver)
        {
            var additionalBindings = new HashSet<BindingMetadata>();
            var expressionStrategy = new BindingExpressionStrategy(semanticModel, typeResolver, new AsIsBindingResultStrategy(), new List<MemberDeclarationSyntax>());
            foreach (var binding in metadata.Bindings)
            {
                foreach (var contractType in binding.ContractTypes)
                {
                    foreach (var tag in binding.Tags.DefaultIfEmpty(null))
                    {
                        expressionStrategy.TryBuild(binding, contractType, tag, new NameService(), additionalBindings);
                    }
                }
            }

            var additionalMembers = new List<MemberDeclarationSyntax>();
            var genericExpressionStrategy = new BindingExpressionStrategy(semanticModel, typeResolver, new GenericBindingResultStrategy(), additionalMembers);
            var genericStatementsStrategy = new TypeBindingStatementsStrategy(genericExpressionStrategy);
            var genericTagStatementsStrategy = new TypeAndTagBindingStatementsStrategy(semanticModel, genericExpressionStrategy);
            var typeOfTExpression = SyntaxFactory.TypeOfExpression(TTypeSyntax);
            var genericReturnDefault = SyntaxFactory.ReturnStatement(SyntaxFactory.DefaultExpression(TTypeSyntax));

            var statementsStrategy = new TypeBindingStatementsStrategy(expressionStrategy);
            var tagStatementsStrategy = new TypeAndTagBindingStatementsStrategy(semanticModel, expressionStrategy);
            var typeExpression = SyntaxFactory.ParseName("type");
            var returnDefault = SyntaxFactory.ReturnStatement(SyntaxFactory.DefaultExpression(ObjectTypeSyntax));

            var allVariants = new[]
            {
                new MethodVariant(GenericStaticResolveMethodSyntax, true, genericStatementsStrategy, typeOfTExpression, genericReturnDefault),
                new MethodVariant(GenericStaticResolveWithTagMethodSyntax, false, genericTagStatementsStrategy, typeOfTExpression, genericReturnDefault),
                new MethodVariant(StaticResolveMethodSyntax, true, statementsStrategy, typeExpression, returnDefault),
                new MethodVariant(StaticResolveWithTagMethodSyntax, false, tagStatementsStrategy, typeExpression, returnDefault)
            };

            var nameService = new NameService();
            
            var variants =
                from binding in metadata.Bindings.Concat(additionalBindings).Distinct()
                from contractType in binding.ContractTypes
                where contractType.IsValidTypeToResolve(semanticModel)
                from tag in binding.Tags.DefaultIfEmpty(null)
                from variant in allVariants
                where variant.HasDefaultTag == (tag == null)
                let statement = ResolveStatement(semanticModel, contractType, variant, binding, nameService)
                group (variant, statement) by (variant.TargetMethod.ToString(), statement.ToString()) into groupedByStatement
                // Avoid duplication of statements
                select groupedByStatement.First();

            foreach (var (variant, statement) in variants)
            {
                variant.TargetMethod = variant.TargetMethod.AddBodyStatements(statement);
            }

            return allVariants
                .Select(strategy => (MemberDeclarationSyntax)strategy.TargetMethod.AddBodyStatements(strategy.DefaultReturnStatement))
                .Concat(additionalMembers);
        }

        private static IfStatementSyntax ResolveStatement(
            SemanticModel semanticModel,
            INamedTypeSymbol contractType,
            MethodVariant method,
            BindingMetadata binding,
            INameService nameService) =>
            SyntaxFactory.IfStatement(
                SyntaxFactory.BinaryExpression(
                    SyntaxKind.EqualsExpression,
                    SyntaxFactory.TypeOfExpression(contractType.ToTypeSyntax(semanticModel)),
                    method.TypeExpression),
                SyntaxFactory.Block(method.BindingStatementsStrategy.CreateStatements(binding, contractType, nameService))
            );
    }
}