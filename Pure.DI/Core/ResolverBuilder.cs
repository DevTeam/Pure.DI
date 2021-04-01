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
                .AddAttributeLists(SyntaxFactory.AttributeList().AddAttributes(AggressiveOptimizationAndInliningAttr));

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

        private static IEnumerable<MemberDeclarationSyntax> CreateResolveMethods(ResolverMetadata metadata, SemanticModel semanticModel, ITypeResolver typeResolver)
        {
            var additionalMembers = new List<MemberDeclarationSyntax>();
            var expressionStrategy = new BindingExpressionStrategy(semanticModel, typeResolver, additionalMembers);
            var statementsStrategy = new TypeBindingStatementsStrategy(expressionStrategy);
            var tagStatementsStrategy = new TypeAndTagBindingStatementsStrategy(semanticModel, expressionStrategy);

            var additionalBindings = new HashSet<BindingMetadata>();
            foreach (var binding in metadata.Bindings)
            {
                foreach (var contractType in binding.ContractTypes)
                {
                    foreach (var tag in binding.Tags.DefaultIfEmpty(null))
                    {
                        expressionStrategy.TryBuild(binding, contractType, tag, additionalBindings);
                    }
                }
            }

            var members = new[]
            {
                StaticResolveMethodSyntax,
                StaticResolveMethodSyntax.AddParameterListParameters(SyntaxFactory.Parameter(SyntaxFactory.Identifier("tag")).WithType(ObjectTypeSyntax))
            };

            var _generated = new HashSet<string>();
            foreach (var binding in metadata.Bindings.Concat(additionalBindings))
            {
                foreach (var contractType in binding.ContractTypes)
                {
                    foreach (var tag in binding.Tags.DefaultIfEmpty(null))
                    {
                        if (!contractType.IsValidTypeToResolve(semanticModel))
                        {
                            continue;
                        }

                        IBindingStatementsStrategy bindingStatementsStrategy;
                        int memberIndex;
                        if (tag == null)
                        {
                            memberIndex = 0;
                            bindingStatementsStrategy = statementsStrategy;
                        }
                        else
                        {
                            memberIndex = 1;
                            bindingStatementsStrategy = tagStatementsStrategy;
                        }

                        var resolveStatementExpression = SyntaxFactory.IfStatement(
                            SyntaxFactory.BinaryExpression(
                                SyntaxKind.EqualsExpression,
                                SyntaxFactory.TypeOfExpression(contractType.ToTypeSyntax(semanticModel)),
                                SyntaxFactory.TypeOfExpression(TTypeSyntax)),
                            SyntaxFactory.Block(bindingStatementsStrategy.CreateStatements(binding, contractType))
                        );

                        if (_generated.Add(resolveStatementExpression.ToString()))
                        {
                            members[memberIndex] = members[memberIndex].AddBodyStatements(resolveStatementExpression);
                        }
                    }
                }
            }

            var returnDefault = SyntaxFactory.ReturnStatement(SyntaxFactory.DefaultExpression(TTypeSyntax));
            for (var memberIndex = 0; memberIndex < members.Length; memberIndex++)
            {
                members[memberIndex] = members[memberIndex].AddBodyStatements(returnDefault);
            }

            var result = new List<MemberDeclarationSyntax>(members);
            result.AddRange(additionalMembers);
            return result;
        }
    }
}