// ReSharper disable InvertIf
namespace Pure.DI.Core;

using NS35EBD81B;

// ReSharper disable once ClassNeverInstantiated.Global
[SuppressMessage("ReSharper", "RedundantTypeArgumentsOfMethod")]
internal class ResolversBuilder : IMembersBuilder
{
    private readonly ResolverMetadata _metadata;
    private readonly IResolveMethodBuilder[] _resolveMethodBuilders;
    private readonly IBuildContext _buildContext;
    private readonly IMemberNameService _memberNameService;
    private readonly IBuildStrategy _buildStrategy;
    private readonly ILog<ResolversBuilder> _log;
    private readonly ITypeResolver _typeResolver;
    private readonly ICannotResolveExceptionFactory _cannotResolveExceptionFactory;
    private readonly ITracer _tracer;
    private readonly IStatementsFinalizer _statementsFinalizer;
    private const string Comments = "\n\t//- - - - - - - - - - - - - - - - - - - - - - - - -";

    public ResolversBuilder(
        ResolverMetadata metadata,
        IResolveMethodBuilder[] resolveMethodBuilders,
        IBuildContext buildContext,
        IMemberNameService memberNameService,
        IBuildStrategy buildStrategy,
        ILog<ResolversBuilder> log,
        ITypeResolver typeResolver,
        ICannotResolveExceptionFactory cannotResolveExceptionFactory,
        ITracer tracer,
        IStatementsFinalizer statementsFinalizer)
    {
        _metadata = metadata;
        _resolveMethodBuilders = resolveMethodBuilders;
        _buildContext = buildContext;
        _memberNameService = memberNameService;
        _buildStrategy = buildStrategy;
        _log = log;
        _typeResolver = typeResolver;
        _cannotResolveExceptionFactory = cannotResolveExceptionFactory;
        _tracer = tracer;
        _statementsFinalizer = statementsFinalizer;
    }

    public int Order => 0;

    public IEnumerable<MemberDeclarationSyntax> BuildMembers(SemanticModel semanticModel)
    {
        var items = (
                from binding in _metadata.Bindings.Concat(_buildContext.AdditionalBindings)
                from dependency in binding.Dependencies
                where dependency.IsValidTypeToResolve
                from tag in binding.GetTags(dependency).DefaultIfEmpty<ExpressionSyntax?>(null)
                group (binding, dependency, tag) by (dependency, tag)
                into grouped
                    // Avoid duplication of statements
                select grouped.Last())
            .ToArray();

        foreach (var member in CreateDependencyTable(items))
        {
            yield return member;
        }

        foreach (var member in CreateDependencyWithTagTable(items))
        {
            yield return member;
        }

        var methods = _resolveMethodBuilders
            .Select(i => i.Build())
            .Where(_ => !_buildContext.IsCancellationRequested)
            .Select(i => i.TargetMethod.AddBodyStatements(i.PostStatements))
            .Select(i => i.WithBody(_statementsFinalizer.AddFinalizationStatements(i.Body)))
            .Concat(_buildContext.AdditionalMembers);

        foreach (var member in methods)
        {
            yield return member;
        }
    }

    private IEnumerable<MemberDeclarationSyntax> CreateDependencyTable(IEnumerable<(IBindingMetadata binding, SemanticType dependency, ExpressionSyntax? tag)> items)
    {
        var keyValuePairType = SyntaxFactory.GenericName(SyntaxFactory.Identifier($"{Defaults.DefaultNamespace}.Pair"))
            .AddTypeArgumentListArguments(SyntaxRepo.TypeTypeSyntax, SyntaxRepo.FuncOfObjectTypeSyntax);

        var keyValuePairs = new List<ExpressionSyntax>();
        foreach (var (binding, resolvingType, resolvingTag) in items)
        {
            if (_buildContext.IsCancellationRequested)
            {
                _log.Trace(() => new[]
                {
                    "Build canceled"
                });

                break;
            }

            if (resolvingTag != null)
            {
                continue;
            }

            var statements = CreateStatements(_buildStrategy, binding, resolvingType, resolvingTag).ToArray();
            if (!statements.Any())
            {
                continue;
            }

            var keyValuePair = SyntaxFactory.ObjectCreationExpression(keyValuePairType)
                .AddArgumentListArguments(
                    SyntaxFactory.Argument(SyntaxFactory.TypeOfExpression(resolvingType.TypeSyntax))
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

    var resolversTableTypeSyntax = SyntaxFactory.ParseTypeName(typeof(ResolversTable).FullName.ReplaceNamespace());
    var resolversTable = SyntaxFactory.FieldDeclaration(
                SyntaxFactory.VariableDeclaration(resolversTableTypeSyntax)
                    .AddVariables(
                        SyntaxFactory.VariableDeclarator(_memberNameService.GetName(MemberNameKind.FactoriesField))
                            .WithInitializer(
                                SyntaxFactory.EqualsValueClause(
                                    SyntaxFactory.ObjectCreationExpression(resolversTableTypeSyntax)
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

    private IEnumerable<MemberDeclarationSyntax> CreateDependencyWithTagTable(IEnumerable<(IBindingMetadata binding, SemanticType dependency, ExpressionSyntax? tag)> items)
    {
        var tagTypeTypeSyntax = SyntaxFactory.ParseTypeName(typeof(TagKey).FullName.ReplaceNamespace());
        var keyValuePairType = SyntaxFactory.GenericName(SyntaxFactory.Identifier($"{Defaults.DefaultNamespace}.Pair"))
            .AddTypeArgumentListArguments(tagTypeTypeSyntax, SyntaxRepo.FuncOfObjectTypeSyntax);

        var keyValuePairs = new List<ExpressionSyntax>();
        foreach (var (binding, resolvingType, resolvingTag) in items)
        {
            if (_buildContext.IsCancellationRequested)
            {
                _log.Trace(() => new[]
                {
                    "Build canceled"
                });

                break;
            }

            if (resolvingTag == null)
            {
                continue;
            }

            var key = SyntaxFactory.ObjectCreationExpression(tagTypeTypeSyntax)
                .AddArgumentListArguments(
                    SyntaxFactory.Argument(SyntaxFactory.TypeOfExpression(resolvingType.TypeSyntax)),
                    SyntaxFactory.Argument(resolvingTag));

            var statements = CreateStatements(_buildStrategy, binding, resolvingType, resolvingTag).ToArray();
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

    var resolversWithTagTableTypeSyntax = SyntaxFactory.ParseTypeName(typeof(ResolversByTagTable).FullName.ReplaceNamespace());
    var resolversTable = SyntaxFactory.FieldDeclaration(
                SyntaxFactory.VariableDeclaration(resolversWithTagTableTypeSyntax)
                    .AddVariables(
                        SyntaxFactory.VariableDeclarator(_memberNameService.GetName(MemberNameKind.FactoriesByTagField))
                            .WithInitializer(SyntaxFactory.EqualsValueClause(SyntaxFactory.ObjectCreationExpression(resolversWithTagTableTypeSyntax)
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

    private IEnumerable<StatementSyntax> CreateStatements(
        IBuildStrategy buildStrategy,
        IBindingMetadata binding,
        SemanticType resolvingType,
        ExpressionSyntax? resolvingTag)
    {
        _tracer.Reset();
        try
        {
            var dependency = _typeResolver.Resolve(resolvingType, resolvingTag);
            var instance = buildStrategy.TryBuild(dependency, resolvingType);
            if (!instance.HasValue)
            {
                if (binding.FromProbe)
                {
                    yield break;
                }

                // Exclude IServiceProvider
                if (resolvingTag == default && binding.Implementation?.ToString() == "System.IServiceProvider")
                {
                    yield break;
                }

                throw _cannotResolveExceptionFactory.Create(binding, resolvingTag, instance.Description, instance.Location);
            }

            yield return SyntaxFactory.ReturnStatement(instance.Value);
        }
        finally
        {
            _tracer.Reset();
        }
    }
}