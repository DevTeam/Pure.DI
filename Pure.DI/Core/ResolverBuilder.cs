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
    [SuppressMessage("ReSharper", "RedundantTypeArgumentsOfMethod")]
    internal class ResolverBuilder
    {
        private readonly IDefaultValueStrategy _defaultValueStrategy;
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

        public ResolverBuilder(IDefaultValueStrategy defaultValueStrategy)
        {
            _defaultValueStrategy = defaultValueStrategy;
        }

        public CompilationUnitSyntax Build(ResolverMetadata metadata, SemanticModel semanticModel, ITypeResolver typeResolver)
        {
            var ownerClass = (
                from cls in metadata.SetupNode.Ancestors().OfType<ClassDeclarationSyntax>()
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
                if (string.IsNullOrWhiteSpace(metadata.TargetTypeName))
                {
                    var parentClassName = metadata.SetupNode.Ancestors().OfType<ClassDeclarationSyntax>().Select(i => i.Identifier.Text).FirstOrDefault();
                    metadata.TargetTypeName = $"{parentClassName}DI";
                }
            }
            else
            {
                classModifiers.AddRange(ownerClass.Modifiers);
                if (string.IsNullOrWhiteSpace(metadata.TargetTypeName))
                {
                    metadata.TargetTypeName = ownerClass.Identifier.Text;
                }
            }

            var compilationUnit = SyntaxFactory.CompilationUnit()
                .AddUsings(
                    SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System.Runtime.CompilerServices")));

            var originalCompilationUnit = metadata.SetupNode.Ancestors().OfType<CompilationUnitSyntax>().FirstOrDefault();
            if (originalCompilationUnit != null)
            {
                compilationUnit = compilationUnit.AddUsings(originalCompilationUnit.Usings.ToArray());
            }

            NamespaceDeclarationSyntax? prevNamespaceNode = null;
            foreach (var originalNamespaceNode in metadata.SetupNode.Ancestors().OfType<NamespaceDeclarationSyntax>().Reverse())
            {
                var namespaceNode = 
                    SyntaxFactory.NamespaceDeclaration(originalNamespaceNode.Name)
                        .AddUsings(originalNamespaceNode.Usings.ToArray());

                prevNamespaceNode = prevNamespaceNode == null ? namespaceNode : prevNamespaceNode.AddMembers(namespaceNode);
            }

            var resolverClass = SyntaxFactory.ClassDeclaration(metadata.TargetTypeName)
                .AddModifiers(classModifiers.ToArray())
                .AddMembers(
                    SyntaxFactory.FieldDeclaration(
                            SyntaxFactory.VariableDeclaration(ContextTypeSyntax)
                                .AddVariables(
                                    SyntaxFactory.VariableDeclarator(SharedContextName)
                                        .WithInitializer(SyntaxFactory.EqualsValueClause(SyntaxFactory.ObjectCreationExpression(ContextTypeSyntax).AddArgumentListArguments()))))
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

        private IEnumerable<MemberDeclarationSyntax> CreateResolveMethods(ResolverMetadata metadata, SemanticModel semanticModel, ITypeResolver typeResolver)
        {
            var additionalBindings = new HashSet<BindingMetadata>();
            var expressionStrategy = new BindingExpressionStrategy(semanticModel, typeResolver, new AsIsBindingResultStrategy(), new List<MemberDeclarationSyntax>());

            // ReSharper disable once UnusedVariable
            // Find additional bindings
            var probes = (
                from binding in metadata.Bindings
                from contractType in binding.ContractTypes
                where contractType.IsValidTypeToResolve(semanticModel)
                from tag in binding.Tags.DefaultIfEmpty<ExpressionSyntax?>(null)
                select expressionStrategy.TryBuild(binding, contractType, tag, new NameService(), additionalBindings))
                .ToList();

            // Default values
            var additionalMembers = new List<MemberDeclarationSyntax>();
            var genericExpressionStrategy = new BindingExpressionStrategy(semanticModel, typeResolver, new GenericBindingResultStrategy(), additionalMembers);
            var genericStatementsStrategy = new TypeBindingStatementsStrategy(genericExpressionStrategy);
            var genericTagStatementsStrategy = new TypeAndTagBindingStatementsStrategy(genericExpressionStrategy);
            var typeOfTExpression = SyntaxFactory.TypeOfExpression(TTypeSyntax);

            var genericReturnDefault = _defaultValueStrategy.Build(metadata.Factories, TTypeSyntax, typeOfTExpression, SyntaxFactory.DefaultExpression(ObjectTypeSyntax));
            var genericWithTagReturnDefault = _defaultValueStrategy.Build(metadata.Factories, TTypeSyntax, typeOfTExpression, SyntaxFactory.ParseTypeName("tag"));

            var statementsStrategy = new TypeBindingStatementsStrategy(expressionStrategy);
            var tagStatementsStrategy = new TypeAndTagBindingStatementsStrategy(expressionStrategy);
            var typeExpression = SyntaxFactory.ParseName("type");

            var returnDefault = _defaultValueStrategy.Build(metadata.Factories, null,SyntaxFactory.ParseTypeName("type"), SyntaxFactory.DefaultExpression(ObjectTypeSyntax));
            var returnWithTagDefault = _defaultValueStrategy.Build(metadata.Factories, null, SyntaxFactory.ParseTypeName("type"), SyntaxFactory.ParseTypeName("type"));

            var allVariants = new[]
            {
                new MethodVariant(GenericStaticResolveMethodSyntax, true, genericStatementsStrategy, typeOfTExpression, genericReturnDefault),
                new MethodVariant(GenericStaticResolveWithTagMethodSyntax, false, genericTagStatementsStrategy, typeOfTExpression, genericWithTagReturnDefault),
                new MethodVariant(StaticResolveMethodSyntax, true, statementsStrategy, typeExpression, returnDefault),
                new MethodVariant(StaticResolveWithTagMethodSyntax, false, tagStatementsStrategy, typeExpression, returnWithTagDefault)
            };

            var nameService = new NameService();

            var variants =
                from binding in metadata.Bindings.Reverse().Concat(additionalBindings).Distinct()
                from contractType in binding.ContractTypes
                where contractType.IsValidTypeToResolve(semanticModel)
                from tag in binding.Tags.DefaultIfEmpty<ExpressionSyntax?>(null)
                from variant in allVariants
                where variant.HasDefaultTag == (tag == null)
                let statement = ResolveStatement(semanticModel, contractType, variant, binding, nameService)
                group (variant, statement) by (variant.TargetMethod.ToString(), statement.ToString(), contractType.ToString(), tag?.ToString()) into groupedByStatement
                // Avoid duplication of statements
                select groupedByStatement.First();

            // Body
            foreach (var (variant, statement) in variants)
            {
                variant.TargetMethod = variant.TargetMethod
                    .AddBodyStatements(statement);
            }

            // Post statements
            foreach (var variant in allVariants)
            {
                variant.TargetMethod = variant.TargetMethod
                    .AddBodyStatements(variant.PostStatements);
            }

            return allVariants
                .Select(strategy => strategy.TargetMethod)
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