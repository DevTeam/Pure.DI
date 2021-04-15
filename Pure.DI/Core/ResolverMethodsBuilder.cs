// ReSharper disable InvertIf
namespace Pure.DI.Core
{
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    // ReSharper disable once ClassNeverInstantiated.Global
    [SuppressMessage("ReSharper", "RedundantTypeArgumentsOfMethod")]
    internal class ResolverMethodsBuilder : IResolverMethodsBuilder
    {
        private readonly ResolverMetadata _metadata;
        private readonly IResolveMethodBuilder[] _resolveMethodBuilders;
        private readonly IBuildContext _buildContext;
        private readonly IBuildStrategy _buildStrategy;
        private readonly IBindingStatementsStrategy _bindingStatementsStrategy;
        private readonly IBindingStatementsStrategy _tagBindingStatementsStrategy;

        public ResolverMethodsBuilder(
            ResolverMetadata metadata,
            IResolveMethodBuilder[] resolveMethodBuilders,
            IBuildContext buildContext,
            [Tag(Tags.SimpleBuildStrategy)] IBuildStrategy buildStrategy,
            [Tag(Tags.TypeStatementsStrategy)] IBindingStatementsStrategy bindingStatementsStrategy,
            [Tag(Tags.TypeAndTagStatementsStrategy)] IBindingStatementsStrategy tagBindingStatementsStrategy)
        {
            _metadata = metadata;
            _resolveMethodBuilders = resolveMethodBuilders;
            _buildContext = buildContext;
            _buildStrategy = buildStrategy;
            _bindingStatementsStrategy = bindingStatementsStrategy;
            _tagBindingStatementsStrategy = tagBindingStatementsStrategy;
        }

        public IEnumerable<MemberDeclarationSyntax> CreateResolveMethods(SemanticModel semanticModel)
        {
            var items = (
                from binding in _metadata.Bindings.Reverse().Concat(_buildContext.AdditionalBindings).Distinct().ToList()
                orderby binding.Weight descending
                from dependency in binding.Dependencies
                where dependency.IsValidTypeToResolve
                from tag in binding.Tags.DefaultIfEmpty<ExpressionSyntax?>(null)
                group (binding, dependency, tag) by (dependency, tag) into grouped
                    // Avoid duplication of statements
                select grouped.First())
                .ToArray();

            yield return CreateResolversTable(items);
            yield return CreateResolversWithTagTable(items);

            var allMethods = _resolveMethodBuilders.Select(i => i.Build(semanticModel)).ToArray();

            // Post statements
            foreach (var method in allMethods)
            {
                method.TargetMethod = method.TargetMethod
                    .AddBodyStatements(method.PostStatements);
            }

            List<MemberDeclarationSyntax> internalMembers = new();
            if (_buildContext.FinalizationStatements.Any())
            {
                const string deepnessFieldName = "_deepness";
                var deepnessField = SyntaxFactory.FieldDeclaration(
                        SyntaxFactory.VariableDeclaration(SyntaxFactory.IdentifierName("int"))
                            .AddVariables(
                                SyntaxFactory.VariableDeclarator(deepnessFieldName)
                            )
                    )
                    .AddModifiers(
                        SyntaxFactory.Token(SyntaxKind.PrivateKeyword),
                        SyntaxFactory.Token(SyntaxKind.StaticKeyword));

                internalMembers.Add(deepnessField);

                var refToDeepness = SyntaxFactory.Argument(SyntaxFactory.IdentifierName(deepnessFieldName)).WithRefKindKeyword(SyntaxFactory.Token(SyntaxKind.RefKeyword));

                var incrementStatement = SyntaxFactory.InvocationExpression(
                    SyntaxFactory.IdentifierName("System.Threading.Interlocked.Increment"),
                    SyntaxFactory.ArgumentList().AddArguments(refToDeepness));

                var decrementStatement = SyntaxFactory.InvocationExpression(
                    SyntaxFactory.IdentifierName("System.Threading.Interlocked.Decrement"),
                    SyntaxFactory.ArgumentList().AddArguments(refToDeepness));

                foreach (var method in allMethods)
                {
                    var curStatements = method.TargetMethod.Body;
                    if (curStatements != null)
                    {
                        var releaseBlock = SyntaxFactory.Block()
                            .AddStatements(SyntaxFactory.ExpressionStatement(decrementStatement))
                            .AddStatements(
                                SyntaxFactory.IfStatement(
                                    SyntaxFactory.BinaryExpression(SyntaxKind.EqualsExpression, SyntaxFactory.IdentifierName(deepnessFieldName), SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(0))),
                                        SyntaxFactory.Block().AddStatements(_buildContext.FinalizationStatements.ToArray())
                                ));

                        var tryStatement = SyntaxFactory.TryStatement(
                            curStatements,
                            SyntaxFactory.List<CatchClauseSyntax>(),
                            SyntaxFactory.FinallyClause(releaseBlock));

                        method.TargetMethod = method.TargetMethod.WithBody(
                            SyntaxFactory.Block()
                                .AddStatements(SyntaxFactory.ExpressionStatement(incrementStatement))
                                .AddStatements(tryStatement));
                    }
                }
            }

            var members =
                allMethods
                .Select(strategy => strategy.TargetMethod)
                .Concat(_buildContext.AdditionalMembers)
                .Concat(internalMembers);

            foreach (var member in members)
            {
                yield return member;
            }
        }

        private FieldDeclarationSyntax CreateResolversTable((BindingMetadata binding, SemanticType dependency, ExpressionSyntax? tag)[] items)
        {
            var funcType = SyntaxFactory.GenericName(
                    SyntaxRepo.FuncTypeToken)
                .AddTypeArgumentListArguments(SyntaxRepo.ObjectTypeSyntax);

            var keyValuePairType = SyntaxFactory.GenericName(
                    SyntaxRepo.KeyValuePairTypeToken)
                .AddTypeArgumentListArguments(SyntaxRepo.TypeTypeSyntax, funcType);

            var keyValuePairs = new List<ExpressionSyntax>();
            foreach (var item in items)
            {
                if (item.tag != null)
                {
                    continue;
                }

                var statements = _bindingStatementsStrategy.CreateStatements(_buildStrategy, item.binding, item.dependency);
                var keyValuePair = SyntaxFactory.ObjectCreationExpression(keyValuePairType)
                    .AddArgumentListArguments(
                        SyntaxFactory.Argument(SyntaxFactory.TypeOfExpression(item.dependency.TypeSyntax)),
                        SyntaxFactory.Argument(SyntaxFactory.ParenthesizedLambdaExpression()
                            .WithBody(SyntaxFactory.Block(statements))));

                keyValuePairs.Add(keyValuePair);
            }

            var arr = SyntaxFactory.ArrayCreationExpression(
                    SyntaxFactory.ArrayType(keyValuePairType),
                    SyntaxFactory.InitializerExpression(SyntaxKind.ArrayInitializerExpression).AddExpressions(keyValuePairs.ToArray()))
                .AddTypeRankSpecifiers(SyntaxFactory.ArrayRankSpecifier());

            var resolversTable = SyntaxFactory.FieldDeclaration(
                    SyntaxFactory.VariableDeclaration(SyntaxRepo.ResolversTableTypeSyntax)
                        .AddVariables(
                            SyntaxFactory.VariableDeclarator(SyntaxRepo.ResolversTableName)
                                .WithInitializer(SyntaxFactory.EqualsValueClause(SyntaxFactory.ObjectCreationExpression(SyntaxRepo.ResolversTableTypeSyntax).AddArgumentListArguments(SyntaxFactory.Argument(arr))))))
                .AddModifiers(
                    SyntaxFactory.Token(SyntaxKind.PrivateKeyword),
                    SyntaxFactory.Token(SyntaxKind.StaticKeyword),
                    SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword));

            return resolversTable;
        }

        private FieldDeclarationSyntax CreateResolversWithTagTable((BindingMetadata binding, SemanticType dependency, ExpressionSyntax? tag)[] items)
        {
            var funcType = SyntaxFactory.GenericName(
                    SyntaxRepo.FuncTypeToken)
                .AddTypeArgumentListArguments(SyntaxRepo.ObjectTypeSyntax);

            var keyValuePairType = SyntaxFactory.GenericName(
                    SyntaxRepo.KeyValuePairTypeToken)
                .AddTypeArgumentListArguments(SyntaxRepo.TagTypeTypeSyntax, funcType);

            var keyValuePairs = new List<ExpressionSyntax>();
            foreach (var item in items)
            {
                if (item.tag == null)
                {
                    continue;
                }

                var key = SyntaxFactory.ObjectCreationExpression(SyntaxRepo.TagTypeTypeSyntax)
                    .AddArgumentListArguments(
                        SyntaxFactory.Argument(SyntaxFactory.TypeOfExpression(item.dependency.TypeSyntax)),
                        SyntaxFactory.Argument(item.tag));

                var statements = _tagBindingStatementsStrategy.CreateStatements(_buildStrategy, item.binding, item.dependency);
                var keyValuePair = SyntaxFactory.ObjectCreationExpression(keyValuePairType)
                    .AddArgumentListArguments(
                        SyntaxFactory.Argument(key),
                        SyntaxFactory.Argument(SyntaxFactory.ParenthesizedLambdaExpression()
                            .WithBody(SyntaxFactory.Block(statements))));

                keyValuePairs.Add(keyValuePair);
            }

            var arr = SyntaxFactory.ArrayCreationExpression(
                    SyntaxFactory.ArrayType(keyValuePairType),
                    SyntaxFactory.InitializerExpression(SyntaxKind.ArrayInitializerExpression).AddExpressions(keyValuePairs.ToArray()))
                .AddTypeRankSpecifiers(SyntaxFactory.ArrayRankSpecifier());

            var resolversTable = SyntaxFactory.FieldDeclaration(
                    SyntaxFactory.VariableDeclaration(SyntaxRepo.ResolversWithTagTableTypeSyntax)
                        .AddVariables(
                            SyntaxFactory.VariableDeclarator(SyntaxRepo.ResolversWithTagTableName)
                                .WithInitializer(SyntaxFactory.EqualsValueClause(SyntaxFactory.ObjectCreationExpression(SyntaxRepo.ResolversWithTagTableTypeSyntax).AddArgumentListArguments(SyntaxFactory.Argument(arr))))))
                .AddModifiers(
                    SyntaxFactory.Token(SyntaxKind.PrivateKeyword),
                    SyntaxFactory.Token(SyntaxKind.StaticKeyword),
                    SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword));

            return resolversTable;
        }
    }
}
