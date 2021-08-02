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
    internal class ResolversBuilder : IMembersBuilder
    {
        private readonly ResolverMetadata _metadata;
        private readonly IResolveMethodBuilder[] _resolveMethodBuilders;
        private readonly IBuildContext _buildContext;
        private readonly IMemberNameService _memberNameService;
        private readonly IBuildStrategy _buildStrategy;
        private readonly IBindingStatementsStrategy _bindingStatementsStrategy;
        private readonly IBindingStatementsStrategy _tagBindingStatementsStrategy;
        private readonly ILog<ResolversBuilder> _log;
        private const string Comments = "\n\t//- - - - - - - - - - - - - - - - - - - - - - - - -";

        public ResolversBuilder(
            ResolverMetadata metadata,
            IResolveMethodBuilder[] resolveMethodBuilders,
            IBuildContext buildContext,
            IMemberNameService memberNameService,
            IBuildStrategy buildStrategy,
            [Tag(Tags.TypeStatementsStrategy)] IBindingStatementsStrategy bindingStatementsStrategy,
            [Tag(Tags.TypeAndTagStatementsStrategy)] IBindingStatementsStrategy tagBindingStatementsStrategy,
            ILog<ResolversBuilder> log)
        {
            _metadata = metadata;
            _resolveMethodBuilders = resolveMethodBuilders;
            _buildContext = buildContext;
            _memberNameService = memberNameService;
            _buildStrategy = buildStrategy;
            _bindingStatementsStrategy = bindingStatementsStrategy;
            _tagBindingStatementsStrategy = tagBindingStatementsStrategy;
            _log = log;
        }

        public int Order => 0;

        public IEnumerable<MemberDeclarationSyntax> BuildMembers(SemanticModel semanticModel)
        {
            var items = (
                from binding in _metadata.Bindings.Concat(_buildContext.AdditionalBindings)
                from dependency in binding.Dependencies
                where dependency.IsValidTypeToResolve
                from tag in binding.Tags.DefaultIfEmpty<ExpressionSyntax?>(null)
                group (binding, dependency, tag) by (dependency, tag) into grouped 
                // Avoid duplication of statements
                select grouped.Last())
                .ToArray();

            foreach (var member in CreateDependencyTable(semanticModel, items))
            {
                yield return member;
            }

            foreach (var member in CreateDependencyWithTagTable(semanticModel, items))
            {
                yield return member;
            }

            var allMethods = _resolveMethodBuilders.Select(i => i.Build()).ToArray();

            // Post statements
            foreach (var method in allMethods)
            {
                method.TargetMethod = method.TargetMethod
                    .AddBodyStatements(method.PostStatements);
            }

            if (!_buildContext.IsCancellationRequested)
            {
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
                        if (_buildContext.IsCancellationRequested)
                        {
                            _log.Trace(() => new[] { "Build canceled" });
                            break;
                        }

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
            else
            {
                _log.Trace(() => new[] { "Build canceled" });
            }
        }

        private IEnumerable<MemberDeclarationSyntax> CreateDependencyTable(SemanticModel semanticModel, IEnumerable<(BindingMetadata binding, SemanticType dependency, ExpressionSyntax? tag)> items)
        {
            var funcType = SyntaxFactory.GenericName(
                    SyntaxRepo.FuncTypeToken)
                .AddTypeArgumentListArguments(SyntaxRepo.ObjectTypeSyntax);

            var keyValuePairType = SyntaxFactory.GenericName(
                    SyntaxRepo.KeyValuePairTypeToken)
                .AddTypeArgumentListArguments(SyntaxRepo.TypeTypeSyntax, funcType);

            var keyValuePairs = new List<ExpressionSyntax>();
            foreach (var (binding, dependency, tag) in items)
            {
                if (_buildContext.IsCancellationRequested)
                {
                    _log.Trace(() => new[] { "Build canceled" });
                    break;
                }

                if (tag != null)
                {
                    continue;
                }

                var statements = _bindingStatementsStrategy.CreateStatements(_buildStrategy, binding, dependency).ToArray();
                if (!statements.Any())
                {
                    continue;
                }

                var keyValuePair = SyntaxFactory.ObjectCreationExpression(keyValuePairType)
                    .AddArgumentListArguments(
                        SyntaxFactory.Argument(SyntaxFactory.TypeOfExpression(dependency.TypeSyntax))
                            .WithCommentBefore(Comments),
                        SyntaxFactory.Argument(SyntaxFactory.ParenthesizedLambdaExpression()
                            .WithBody(SyntaxFactory.Block(statements)))
                            .WithCommentBefore(Comments))
                    .WithCommentBefore(Comments);

                keyValuePairs.Add(keyValuePair);
            }

            var arr = SyntaxFactory.ArrayCreationExpression(
                    SyntaxFactory.ArrayType(keyValuePairType),
                    SyntaxFactory.InitializerExpression(SyntaxKind.ArrayInitializerExpression).AddExpressions(keyValuePairs.ToArray()))
                .AddTypeRankSpecifiers(SyntaxFactory.ArrayRankSpecifier());

            var resolversTable = SyntaxFactory.FieldDeclaration(
                    SyntaxFactory.VariableDeclaration(SyntaxRepo.ResolversTableTypeSyntax)
                        .AddVariables(
                            SyntaxFactory.VariableDeclarator(_memberNameService.GetName(MemberNameKind.FactoriesField))
                                .WithInitializer(
                                    SyntaxFactory.EqualsValueClause(
                                        SyntaxFactory.ObjectCreationExpression(SyntaxRepo.ResolversTableTypeSyntax)
                                            .AddArgumentListArguments(
                                                SyntaxFactory.Argument(arr))))))
                .AddModifiers(
                    SyntaxFactory.Token(SyntaxKind.PrivateKeyword),
                    SyntaxFactory.Token(SyntaxKind.StaticKeyword),
                    SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword));

            yield return resolversTable;

            var divisor = SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(ResolversTable.GetDivisor(keyValuePairs.Count)));
            yield return CreateField(SyntaxRepo.UIntTypeSyntax, nameof(ResolversTable.ResolversDivisor), divisor, SyntaxKind.ConstKeyword);
            var bucketsType = SyntaxFactory.ArrayType(keyValuePairType).AddRankSpecifiers(SyntaxFactory.ArrayRankSpecifier());
            yield return CreateField(bucketsType, nameof(ResolversTable.ResolversBuckets), GetFiled(_memberNameService.GetName(MemberNameKind.FactoriesField), nameof(ResolversTable.ResolversBuckets)), SyntaxKind.StaticKeyword, SyntaxKind.ReadOnlyKeyword);
        }

        private IEnumerable<MemberDeclarationSyntax> CreateDependencyWithTagTable(SemanticModel semanticModel, IEnumerable<(BindingMetadata binding, SemanticType dependency, ExpressionSyntax? tag)> items)
        {
            var funcType = SyntaxFactory.GenericName(
                    SyntaxRepo.FuncTypeToken)
                .AddTypeArgumentListArguments(SyntaxRepo.ObjectTypeSyntax);

            var keyValuePairType = SyntaxFactory.GenericName(
                    SyntaxRepo.KeyValuePairTypeToken)
                .AddTypeArgumentListArguments(SyntaxRepo.TagTypeTypeSyntax, funcType);

            var keyValuePairs = new List<ExpressionSyntax>();
            foreach (var (binding, dependency, tag) in items)
            {
                if (_buildContext.IsCancellationRequested)
                {
                    _log.Trace(() => new[] { "Build canceled" });
                    break;
                }

                if (tag == null)
                {
                    continue;
                }

                var key = SyntaxFactory.ObjectCreationExpression(SyntaxRepo.TagTypeTypeSyntax)
                    .AddArgumentListArguments(
                        SyntaxFactory.Argument(SyntaxFactory.TypeOfExpression(dependency.TypeSyntax)),
                        SyntaxFactory.Argument(tag));

                var statements = _tagBindingStatementsStrategy.CreateStatements(_buildStrategy, binding, dependency).ToArray();
                if (!statements.Any())
                {
                    continue;
                }

                var keyValuePair = SyntaxFactory.ObjectCreationExpression(keyValuePairType)
                    .AddArgumentListArguments(
                        SyntaxFactory.Argument(key)
                            .WithCommentBefore(Comments),
                        SyntaxFactory.Argument(SyntaxFactory.ParenthesizedLambdaExpression()
                            .WithBody(SyntaxFactory.Block(statements)))
                            .WithCommentBefore(Comments))
                    .WithCommentBefore(Comments);

                keyValuePairs.Add(keyValuePair);
            }

            var arr = SyntaxFactory.ArrayCreationExpression(
                    SyntaxFactory.ArrayType(keyValuePairType),
                    SyntaxFactory.InitializerExpression(SyntaxKind.ArrayInitializerExpression).AddExpressions(keyValuePairs.ToArray()))
                .AddTypeRankSpecifiers(SyntaxFactory.ArrayRankSpecifier());

            var resolversTable = SyntaxFactory.FieldDeclaration(
                    SyntaxFactory.VariableDeclaration(SyntaxRepo.ResolversWithTagTableTypeSyntax)
                        .AddVariables(
                            SyntaxFactory.VariableDeclarator(_memberNameService.GetName(MemberNameKind.FactoriesByTagField))
                                .WithInitializer(SyntaxFactory.EqualsValueClause(SyntaxFactory.ObjectCreationExpression(SyntaxRepo.ResolversWithTagTableTypeSyntax)
                                    .AddArgumentListArguments(
                                        SyntaxFactory.Argument(SyntaxFactory.IdentifierName(_memberNameService.GetName(MemberNameKind.FactoriesField))),
                                        SyntaxFactory.Argument(arr))))))
                .AddModifiers(
                    SyntaxFactory.Token(SyntaxKind.PrivateKeyword),
                    SyntaxFactory.Token(SyntaxKind.StaticKeyword),
                    SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword));

            yield return resolversTable;

            var divisor = SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(ResolversTable.GetDivisor(keyValuePairs.Count)));
            yield return CreateField(SyntaxRepo.UIntTypeSyntax, nameof(ResolversByTagTable.ResolversByTagDivisor), divisor, SyntaxKind.ConstKeyword);
            var bucketsType = SyntaxFactory.ArrayType(keyValuePairType).AddRankSpecifiers(SyntaxFactory.ArrayRankSpecifier());
            yield return CreateField(bucketsType, nameof(ResolversByTagTable.ResolversByTagBuckets), GetFiled(_memberNameService.GetName(MemberNameKind.FactoriesByTagField), nameof(ResolversByTagTable.ResolversByTagBuckets)), SyntaxKind.StaticKeyword, SyntaxKind.ReadOnlyKeyword);
        }

        private static MemberAccessExpressionSyntax GetFiled(string tableName, string fieldName) =>
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.IdentifierName(tableName),
                SyntaxFactory.IdentifierName(fieldName));

        private static FieldDeclarationSyntax CreateField(TypeSyntax type, string name, ExpressionSyntax initExpression, params SyntaxKind[] modifiers) =>
            SyntaxFactory.FieldDeclaration(
                    SyntaxFactory.VariableDeclaration(type)
                        .AddVariables(
                            SyntaxFactory.VariableDeclarator(name)
                                .WithInitializer(SyntaxFactory.EqualsValueClause(initExpression))))
                .WithModifiers(SyntaxFactory.TokenList()
                    .AddRange(Enumerable.Repeat(SyntaxKind.PrivateKeyword, 1).Concat(modifiers).Select(SyntaxFactory.Token)));
    }
}
