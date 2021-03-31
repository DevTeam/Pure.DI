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
        private readonly Dictionary<string, int> _names = new Dictionary<string, int>();

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

        public CompilationUnitSyntax Build(ResolverMetadata metadata, SemanticModel semanticModel, ITypeResolver typeResolver) =>
            SyntaxFactory.CompilationUnit()
                .AddMembers(
                    SyntaxFactory.NamespaceDeclaration(SyntaxFactory.IdentifierName(metadata.Namespace))
                        .AddUsings(
                            SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(nameof(System))),
                            SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System.Runtime.CompilerServices")))
                        .AddMembers(
                            SyntaxFactory.ClassDeclaration(metadata.TargetTypeName)
                                .AddModifiers(
                                    SyntaxFactory.Token(SyntaxKind.InternalKeyword),
                                    SyntaxFactory.Token(SyntaxKind.StaticKeyword))
                                .AddMembers(CreateResolveMethods(metadata, semanticModel, typeResolver).ToArray())))
                .NormalizeWhitespace();

        private IEnumerable<MemberDeclarationSyntax> CreateResolveMethods(
            ResolverMetadata metadata,
            SemanticModel semanticModel,
            ITypeResolver typeResolver)
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
                members.Add(
                ResolveMethodSyntax.AddBodyStatements(
                    CreateResolveStatements(ByType, simpleBindings, semanticModel, typeResolver, members).ToArray()));
            }

            var tagedBindings = bindings.Where(i => i.Tags.Any()).ToList();
            if (tagedBindings.Any())
            {
                members.Add(
                    ResolveMethodSyntax
                        .AddParameterListParameters(SyntaxFactory.Parameter(SyntaxFactory.Identifier("tag")).WithType(ObjectTypeSyntax))
                        .AddBodyStatements(
                        CreateResolveStatements(ByTypeAndTag, tagedBindings, semanticModel, typeResolver, members).ToArray()));
            }

            return members;
        }

        private IEnumerable<StatementSyntax> CreateResolveStatements(
            Func<BindingMetadata, Func<ExpressionSyntax>, IEnumerable<StatementSyntax>> resolverType,
            IEnumerable<BindingMetadata> bindings,
            SemanticModel semanticModel,
            ITypeResolver typeResolver,
            ICollection<MemberDeclarationSyntax> additionalMembers)
        {
            var statements = new List<StatementSyntax>();
            foreach (var binding in bindings)
            {
                statements.AddRange(ResolveGeneric(resolverType, binding, semanticModel, typeResolver, additionalMembers));
            }

            statements.Add(SyntaxFactory.ReturnStatement(SyntaxFactory.DefaultExpression(TTypeSyntax)));
            return statements;
        }

        private IEnumerable<StatementSyntax> ResolveGeneric(
            Func<BindingMetadata, Func<ExpressionSyntax>, IEnumerable<StatementSyntax>> resolverType,
            BindingMetadata binding,
            SemanticModel semanticModel,
            ITypeResolver typeResolver,
            ICollection<MemberDeclarationSyntax> additionalMembers)
        {
            var statements = new List<StatementSyntax>();
            var typeExpression = SyntaxFactory.TypeOfExpression(TTypeSyntax);
            foreach (var contractType in binding.ContractTypes)
            {
                if (!contractType.IsValidTypeToResolve(semanticModel))
                {
                    continue;
                }

                var instanceStatements = resolverType(binding, () => TryBuild(contractType, semanticModel, binding.Lifetime, typeResolver, additionalMembers)).ToArray();

                var resolveStatementExpression = SyntaxFactory.IfStatement(
                    SyntaxFactory.BinaryExpression(
                        SyntaxKind.EqualsExpression,
                        SyntaxFactory.TypeOfExpression(contractType.ToTypeSyntax(semanticModel)),
                        typeExpression),
                    SyntaxFactory.Block(instanceStatements)
                );

                statements.Add(resolveStatementExpression);
            }

            return statements;
        }

        [CanBeNull] private ExpressionSyntax TryBuild(
            INamedTypeSymbol contractType,
            SemanticModel semanticModel,
            Lifetime lifetime,
            ITypeResolver typeResolver,
            ICollection<MemberDeclarationSyntax> additionalMembers)
        {
            ExpressionSyntax objectExpression = _objectBuilder.TryBuild(contractType, semanticModel, typeResolver);
            if (objectExpression == null)
            {
                return null;
            }

            switch (lifetime)
            {
                case Lifetime.Singleton:
                    {
                        var resolvedType = typeResolver.Resolve(contractType);
                        var singletonClassName = FindName(string.Join("_", resolvedType.ToMinimalDisplayParts(semanticModel, 0).Where(i => i.Kind == SymbolDisplayPartKind.ClassName).Select(i => i.ToString())) + "Singleton");
                        
                        var singletonClass = SyntaxFactory.ClassDeclaration(singletonClassName)
                            .AddModifiers(
                                SyntaxFactory.Token(SyntaxKind.PrivateKeyword),
                                SyntaxFactory.Token(SyntaxKind.StaticKeyword))
                            .AddMembers(
                                SyntaxFactory.FieldDeclaration(
                                    SyntaxFactory.VariableDeclaration(
                                        resolvedType.ToTypeSyntax(semanticModel))
                                        .AddVariables(
                                            SyntaxFactory.VariableDeclarator("Shared")
                                                .WithInitializer(SyntaxFactory.EqualsValueClause(objectExpression))
                                        )
                                    )
                                    .AddModifiers(
                                        SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                                        SyntaxFactory.Token(SyntaxKind.StaticKeyword),
                                        SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword))
                                );

                        additionalMembers.Add(singletonClass);
                        objectExpression = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.ParseName(singletonClassName), SyntaxFactory.Token(SyntaxKind.DotToken), SyntaxFactory.IdentifierName("Shared"));
                        break;
                    }
            }

            return SyntaxFactory.BinaryExpression(SyntaxKind.AsExpression, objectExpression, TTypeSyntax);
        }

        private static IEnumerable<StatementSyntax> ByType(BindingMetadata binding, Func<ExpressionSyntax> objectExpression)
        {
            yield return SyntaxFactory.ReturnStatement(objectExpression());
        }

        private static IEnumerable<StatementSyntax> ByTypeAndTag(BindingMetadata binding, Func<ExpressionSyntax> objectExpression)
        {
            foreach (var tag in binding.Tags)
            {
                var instance = objectExpression();
                var statement = SyntaxFactory.IfStatement(
                    SyntaxFactory.InvocationExpression(
                        SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, tag, SyntaxFactory.Token(SyntaxKind.DotToken), SyntaxFactory.IdentifierName("Equals"))
                    ).AddArgumentListArguments(SyntaxFactory.Argument(SyntaxFactory.IdentifierName("tag"))),
                    SyntaxFactory.Block(SyntaxFactory.ReturnStatement(instance)));

                yield return statement;
            }
        }

        private string FindName(string prefix)
        {
            var name = prefix;
            if (!_names.TryGetValue(prefix, out var id))
            {
                _names.Add(prefix, 0);
            }
            else
            {
                _names[prefix] = id + 1;
                name = name + id;
            }

            return name;
        }
    }
}