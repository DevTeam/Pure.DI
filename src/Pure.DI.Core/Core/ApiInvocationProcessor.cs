// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable ConvertIfStatementToConditionalTernaryExpression

namespace Pure.DI.Core;

using Microsoft.CodeAnalysis.Operations;
using static LinesBuilderExtensions;

sealed class ApiInvocationProcessor(
    IComments comments,
    IArguments arguments,
    ISemantic semantic,
    ISymbolNames symbolNames,
    [Tag(Tag.UniqueTag)] IdGenerator idGenerator,
    IOverrideIdProvider overrideIdProvider,
    IBaseSymbolsProvider baseSymbolsProvider,
    INameFormatter nameFormatter,
    IUniqueNameProvider uniqueNameProvider,
    ITypes types,
    IWildcardMatcher wildcardMatcher,
    Func<IFactoryApiWalker> factoryApiWalkerFactory,
    Func<ILocalVariableRenamingRewriter> localVariableRenamingRewriterFactory,
    ILocationProvider locationProvider)
    : IApiInvocationProcessor
{
    private static readonly char[] TypeNamePartsSeparators = ['.'];

    public void ProcessInvocation(
        IMetadataVisitor metadataVisitor,
        SemanticModel semanticModel,
        InvocationExpressionSyntax invocation,
        string @namespace)
    {
        var name = invocation.Expression switch
        {
            MemberAccessExpressionSyntax memberAccess when memberAccess.Kind() == SyntaxKind.SimpleMemberAccessExpression => memberAccess.Name,
            IdentifierNameSyntax { Identifier.Text: nameof(DI.Setup) } identifierName => identifierName,
            _ => null
        };

        var prevInvocation = invocation.DescendantNodes().FirstOrDefault(i => i is InvocationExpressionSyntax);
        List<string> invocationComments;
        if (prevInvocation is null)
        {
            invocationComments = comments.GetComments(
                invocation.GetLeadingTrivia()
                    .Where(trivia => trivia.IsKind(SyntaxKind.SingleLineCommentTrivia))).ToList();
        }
        else
        {
            invocationComments = comments.GetComments(
                invocation.DescendantTrivia(node => node != prevInvocation, true)
                    .Where(trivia => trivia.IsKind(SyntaxKind.SingleLineCommentTrivia))).ToList();
        }

        switch (name)
        {
            case IdentifierNameSyntax identifierName:
                switch (identifierName.Identifier.Text)
                {
                    case nameof(IConfiguration.Bind):
                        metadataVisitor.VisitContract(
                            new MdContract(
                                semanticModel,
                                invocation,
                                null,
                                ContractKind.Explicit,
                                BuildTags(semanticModel, invocation.ArgumentList.Arguments)));
                        break;

                    case nameof(IBinding.To):
                        switch (invocation.ArgumentList.Arguments)
                        {
                            case [{ Expression: LambdaExpressionSyntax lambdaExpression }]:
                                var type = semantic.TryGetTypeSymbol<ITypeSymbol>(semanticModel, lambdaExpression)
                                           ?? semantic.GetTypeSymbol<ITypeSymbol>(semanticModel, lambdaExpression.Body);

                                if (type is INamedTypeSymbol symbol)
                                {
                                    if (symbol.TypeArguments.Length > 1
                                        && symbolNames.GetGlobalName(symbol.TypeArguments[0]) != Names.IContextTypeName)
                                    {
                                        switch (invocation.ArgumentList.Arguments[0].Expression)
                                        {
                                            case ParenthesizedLambdaExpressionSyntax { ParameterList.Parameters.Count: > 0 } parenthesizedLambda:
                                                VisitSimpleFactory(
                                                    metadataVisitor,
                                                    semanticModel,
                                                    invocation,
                                                    symbol.TypeArguments.Last(),
                                                    parenthesizedLambda.ParameterList.Parameters.Select(i => i.Type!).ToList(),
                                                    parenthesizedLambda);
                                                break;
                                        }

                                        break;
                                    }

                                    if (symbol.TypeArguments.Length == 2 && symbol.TypeArguments is [_, {} resultType])
                                    {
                                        VisitFactory(invocation, metadataVisitor, semanticModel, resultType, lambdaExpression);
                                        break;
                                    }
                                }

                                VisitFactory(invocation, metadataVisitor, semanticModel, type, lambdaExpression);
                                break;

                            case []:
                                break;

                            default:
                                NotSupported(invocation);
                                break;
                        }

                        break;

                    case nameof(IBinding.As):
                        if (invocation.ArgumentList.Arguments is [{ Expression: {} lifetimeExpression }])
                        {
                            metadataVisitor.VisitLifetime(new MdLifetime(semanticModel, invocation, semantic.GetRequiredConstantValue<Lifetime>(semanticModel, lifetimeExpression)));
                        }

                        break;

                    case nameof(IConfiguration.Hint):
                        var hintArgs = arguments.GetArgs(invocation.ArgumentList, "hint", "value");
                        if (hintArgs is [{ Expression: {} hintNameExpression }, { Expression: {} hintValueExpression }])
                        {
                            var values = new LinkedList<string>();
                            values.AddLast(semantic.GetRequiredConstantValue<object>(semanticModel, hintValueExpression, SmartTagKind.Name).ToString());
                            metadataVisitor.VisitHint(new MdHint(semantic.GetConstantValue<Hint>(semanticModel, hintNameExpression), values));
                        }

                        break;

                    case nameof(IBinding.Tags):
                        foreach (var tag in BuildTags(semanticModel, invocation.ArgumentList.Arguments))
                        {
                            metadataVisitor.VisitTag(tag);
                        }

                        break;

                    case nameof(DI.Setup):
                        var setupArgs = arguments.GetArgs(invocation.ArgumentList, "compositionTypeName", "kind");
                        var setupCompositionTypeName = setupArgs[0] is {} compositionTypeNameArg ? semantic.GetRequiredConstantValue<string>(semanticModel, compositionTypeNameArg.Expression) : "";
                        var setupKind = setupArgs[1] is {} setupKindArg ? semantic.GetRequiredConstantValue<CompositionKind>(semanticModel, setupKindArg.Expression) : CompositionKind.Public;
                        if (setupKind != CompositionKind.Global
                            && string.IsNullOrWhiteSpace(setupCompositionTypeName)
                            && invocation.Ancestors().OfType<BaseTypeDeclarationSyntax>().FirstOrDefault() is {} baseType)
                        {
                            setupCompositionTypeName = baseType.Identifier.Text;
                        }

                        foreach (var hint in comments.GetHints(invocationComments))
                        {
                            metadataVisitor.VisitHint(new MdHint(hint.Key, hint.Value));
                        }

                        metadataVisitor.VisitSetup(
                            new MdSetup(
                                semanticModel,
                                invocation,
                                CreateCompositionName(setupCompositionTypeName, @namespace, invocation.ArgumentList),
                                ImmutableArray<MdUsingDirectives>.Empty,
                                setupKind,
                                new Hints(),
                                ImmutableArray<MdBinding>.Empty,
                                ImmutableArray<MdRoot>.Empty,
                                ImmutableArray<MdDependsOn>.Empty,
                                ImmutableArray<MdGenericTypeArgument>.Empty,
                                ImmutableArray<MdGenericTypeArgumentAttribute>.Empty,
                                ImmutableArray<MdTypeAttribute>.Empty,
                                ImmutableArray<MdTagAttribute>.Empty,
                                ImmutableArray<MdOrdinalAttribute>.Empty,
                                ImmutableArray<MdAccumulator>.Empty,
                                [],
                                comments.FilterHints(invocationComments).ToList()));
                        break;

                    case nameof(IConfiguration.DefaultLifetime):
                        if (invocation.ArgumentList.Arguments is [{ Expression: {} defaultLifetimeSyntax }])
                        {
                            metadataVisitor.VisitDefaultLifetime(new MdDefaultLifetime(new MdLifetime(semanticModel, invocation, semantic.GetRequiredConstantValue<Lifetime>(semanticModel, defaultLifetimeSyntax))));
                        }

                        break;

                    case nameof(IConfiguration.DependsOn):
                        if (BuildConstantArgs<string>(semanticModel, invocation.ArgumentList.Arguments) is [..] compositionTypeNames)
                        {
                            metadataVisitor.VisitDependsOn(new MdDependsOn(semanticModel, invocation, compositionTypeNames.Select(i => CreateCompositionName(i, @namespace, invocation.ArgumentList)).ToImmutableArray()));
                        }

                        break;
                }

                break;

            case GenericNameSyntax genericName:
                switch (genericName.Identifier.Text)
                {
                    case nameof(IConfiguration.Bind):
                        VisitBind(metadataVisitor, semanticModel, invocation, genericName);
                        break;

                    case nameof(IConfiguration.RootBind):
                        if (genericName.TypeArgumentList.Arguments is not [{} rootBindType])
                        {
                            throw new CompileErrorException(
                                Strings.Error_InvalidRootType,
                                ImmutableArray.Create(locationProvider.GetLocation(invocation)),
                                LogId.ErrorInvalidMetadata);
                        }

                        var tagArguments = invocation.ArgumentList.Arguments.SkipWhile((arg, i) => arg.NameColon?.Name.Identifier.Text != "tags" && i < 2);
                        var tags = BuildTags(semanticModel, tagArguments);
                        VisitBind(metadataVisitor, semanticModel, invocation, tags, genericName);
                        var rootBindSymbol = semantic.GetTypeSymbol<INamedTypeSymbol>(semanticModel, rootBindType);
                        VisitRoot(invocation, tags.FirstOrDefault(), metadataVisitor, semanticModel, invocation, invocationComments, rootBindSymbol);
                        break;

                    case nameof(IBinding.To):
                        if (genericName.TypeArgumentList.Arguments.Count > 1
                            && invocation.ArgumentList.Arguments.Count == 1)
                        {
                            switch (invocation.ArgumentList.Arguments[0].Expression)
                            {
                                case ParenthesizedLambdaExpressionSyntax { ParameterList.Parameters.Count: > 0 } parenthesizedLambda:
                                    VisitSimpleFactory(
                                        metadataVisitor,
                                        semanticModel,
                                        invocation,
                                        semantic.GetTypeSymbol<ITypeSymbol>(semanticModel, genericName.TypeArgumentList.Arguments.Last()),
                                        genericName.TypeArgumentList.Arguments.Reverse().Skip(1).Reverse().ToList(),
                                        parenthesizedLambda);
                                    break;

                                case SimpleLambdaExpressionSyntax simpleLambda:
                                    VisitSimpleFactory(
                                        metadataVisitor,
                                        semanticModel,
                                        invocation,
                                        semantic.GetTypeSymbol<ITypeSymbol>(semanticModel, genericName.TypeArgumentList.Arguments.Last()),
                                        genericName.TypeArgumentList.Arguments.Reverse().Skip(1).Reverse().ToList(),
                                        simpleLambda);
                                    break;
                            }

                            break;
                        }

                        if (genericName.TypeArgumentList.Arguments is [{} implementationTypeSyntax])
                        {
                            switch (invocation.ArgumentList.Arguments)
                            {
                                case [{ Expression: LambdaExpressionSyntax lambdaExpression }]:
                                    var factoryType = semantic.GetTypeSymbol<ITypeSymbol>(semanticModel, implementationTypeSyntax);
                                    VisitFactory(invocation, metadataVisitor, semanticModel, factoryType, lambdaExpression);
                                    break;

                                case [{ Expression: LiteralExpressionSyntax { Token.Value: string sourceCodeStatement } }]:
                                    var lambda = SyntaxFactory
                                        .SimpleLambdaExpression(SyntaxFactory.Parameter(SyntaxFactory.Identifier("_")))
                                        .WithExpressionBody(SyntaxFactory.IdentifierName(sourceCodeStatement));
                                    factoryType = semantic.GetTypeSymbol<ITypeSymbol>(semanticModel, implementationTypeSyntax);
                                    VisitFactory(invocation, metadataVisitor, semanticModel, factoryType, lambda);
                                    break;

                                case []:
                                    var implementationType = semantic.GetTypeSymbol<INamedTypeSymbol>(semanticModel, implementationTypeSyntax);
                                    metadataVisitor.VisitImplementation(new MdImplementation(semanticModel, invocation, implementationType));
                                    break;

                                default:
                                    NotSupported(invocation);
                                    break;
                            }
                        }

                        break;

                    case nameof(IConfiguration.Arg):
                        VisitArg(invocation, metadataVisitor, semanticModel, ArgKind.Class, invocation, genericName, invocationComments);
                        break;

                    case nameof(IConfiguration.RootArg):
                        VisitArg(invocation, metadataVisitor, semanticModel, ArgKind.Root, invocation, genericName, invocationComments);
                        break;

                    case nameof(IConfiguration.Roots):
                        if (genericName.TypeArgumentList.Arguments is not [{} rootsTypeSyntax]
                            || semantic.TryGetTypeSymbol<INamedTypeSymbol>(semanticModel, rootsTypeSyntax) is not {} rootsType
                            // ReSharper disable once MergeIntoNegatedPattern
                            || rootsType.SpecialType == Microsoft.CodeAnalysis.SpecialType.System_Object)
                        {
                            throw new CompileErrorException(
                                Strings.Error_InvalidRootsRype,
                                ImmutableArray.Create(locationProvider.GetLocation(invocation)),
                                LogId.ErrorInvalidMetadata);
                        }

                        var rootsArgs = arguments.GetArgs(invocation.ArgumentList, "name", "kind", "filter");
                        var rootsName = rootsArgs[0] is {} rootsNameArg ? semantic.GetConstantValue<object>(semanticModel, rootsNameArg.Expression, SmartTagKind.Name)?.ToString() ?? "" : "";
                        var rootsKind = rootsArgs[1] is {} rootsKindArg ? semantic.GetConstantValue<RootKinds>(semanticModel, rootsKindArg.Expression) : RootKinds.Default;
                        var rootsWildcardFilter = (rootsArgs[2] is {} rootsFilterArg ? semantic.GetConstantValue<string>(semanticModel, rootsFilterArg.Expression) : "*") ?? "*";
                        var hasRootsType = false;
                        foreach (var rootType in GetRelatedTypes(invocation, semanticModel, invocation, rootsType, rootsWildcardFilter))
                        {
                            var rootName = GetName((SyntaxNode?)rootsArgs[1] ?? invocation, rootsName, rootType) ?? "";
                            metadataVisitor.VisitRoot(new MdRoot(invocation, semanticModel, rootType, rootName, new MdTag(0, null), rootsKind, invocationComments, false));
                            hasRootsType = true;
                        }

                        if (!hasRootsType)
                        {
                            throw new CompileErrorException(
                                string.Format(Strings.Error_Template_NoTypeForWildcard, symbolNames.GetName(rootsType), rootsWildcardFilter),
                                ImmutableArray.Create(locationProvider.GetLocation(invocation)),
                                LogId.ErrorInvalidMetadata);
                        }

                        break;

                    case nameof(IConfiguration.Root):
                        if (genericName.TypeArgumentList.Arguments is not [{} rootTypeSyntax])
                        {
                            throw new CompileErrorException(
                                Strings.Error_InvalidRootType,
                                ImmutableArray.Create(locationProvider.GetLocation(invocation)),
                                LogId.ErrorInvalidMetadata);
                        }

                        var rootSymbol = semantic.GetTypeSymbol<INamedTypeSymbol>(semanticModel, rootTypeSyntax);
                        VisitRoot(invocation, metadataVisitor, semanticModel, invocation, invocationComments, rootSymbol);
                        break;

                    case nameof(IConfiguration.Builders):
                        if (genericName.TypeArgumentList.Arguments is not [{} buildersRootTypeSyntax]
                            || semantic.TryGetTypeSymbol<INamedTypeSymbol>(semanticModel, buildersRootTypeSyntax) is not {} buildersRootType
                            // ReSharper disable once MergeIntoNegatedPattern
                            || buildersRootType.SpecialType == Microsoft.CodeAnalysis.SpecialType.System_Object)
                        {
                            throw new CompileErrorException(
                                Strings.Error_InvalidBuildersType,
                                ImmutableArray.Create(locationProvider.GetLocation(invocation)),
                                LogId.ErrorInvalidMetadata);
                        }

                        var buildersArgs = arguments.GetArgs(invocation.ArgumentList, "name", "kind", "filter");
                        var buildersName = buildersArgs[0] is {} buildersNameArg ? semantic.GetConstantValue<object>(semanticModel, buildersNameArg.Expression, SmartTagKind.Name)?.ToString() ?? Names.DefaultBuilderName : Names.DefaultBuilderName;
                        var buildersKind = buildersArgs[1] is {} buildersKindArg ? semantic.GetConstantValue<RootKinds>(semanticModel, buildersKindArg.Expression) : RootKinds.Default;
                        var buildersWildcardFilter = (buildersArgs[2] is {} buildersFilterArg ? semantic.GetConstantValue<string>(semanticModel, buildersFilterArg.Expression) : "*") ?? "*";
                        var builderRoots = (
                            from buildersType in GetRelatedTypes(invocation, semanticModel, invocation, buildersRootType, buildersWildcardFilter)
                            let buildersItemName = GetName((SyntaxNode?)buildersArgs[0] ?? invocation, buildersName, buildersType) ?? Names.DefaultBuilderName
                            select VisitBuilder(invocation, metadataVisitor, semanticModel, buildersType, buildersItemName, buildersKind, invocationComments))
                            .ToList();

                        if (builderRoots.Count == 0)
                        {
                            throw new CompileErrorException(
                                string.Format(Strings.Error_Template_NoTypeForWildcard, symbolNames.GetName(buildersRootType), buildersWildcardFilter),
                                ImmutableArray.Create(locationProvider.GetLocation(invocation)),
                                LogId.ErrorInvalidMetadata);
                        }

                        // Composite builder
                        var builderArgId = idGenerator.Generate();
                        var builderArgTag = new MdTag(0, builderArgId + "BuilderArg" + Names.Salt);
                        var builderTag = new MdTag(0, builderArgId + "Builder" + Names.Salt);

                        // Building instance arg
                        metadataVisitor.VisitContract(new MdContract(semanticModel, invocation, buildersRootType, ContractKind.Explicit, ImmutableArray.Create(builderArgTag)));
                        metadataVisitor.VisitArg(new MdArg(semanticModel, invocation, buildersRootType, Names.BuildingInstance, ArgKind.Root, true, ["Instance for the build-up."]));

                        // Factory
                        var factory = new LinesBuilder();
                        factory.AppendLine($"({Names.IContextTypeName} {Names.ContextInstance}) =>");
                        factory.AppendLine(BlockStart);
                        using (factory.Indent())
                        {
                            factory.AppendLine($"{Names.ContextInstance}.{nameof(IContext.Inject)}({builderArgTag.Value.ValueToString()}, out {symbolNames.GetName(buildersRootType)} {Names.BuildingInstance});");
                            factory.AppendLine($"switch ({Names.BuildingInstance})");
                            factory.AppendLine(BlockStart);
                            using (factory.Indent())
                            {
                                foreach (var builderRoot in builderRoots)
                                {
                                    var instanceName = uniqueNameProvider.GetUniqueName($"{nameFormatter.Format("{type}", builderRoot.RootType, builderRoot.Tag?.Value)}");
                                    factory.AppendLine($"case {builderRoot.RootType} {instanceName}:");
                                    using (factory.Indent())
                                    {
                                        factory.AppendLine($"return {builderRoot.Name}({instanceName});");
                                    }
                                }

                                factory.AppendLine("default:");
                                using (factory.Indent())
                                {
                                    factory.AppendLine($"throw new {Names.ArgumentExceptionTypeName}($\"{Names.CannotBuildMessage} {{{Names.BuildingInstance}.GetType()}}.\", \"{Names.BuildingInstance}\");");
                                }
                            }

                            factory.AppendLine(BlockFinish);
                            factory.AppendLine($"return {Names.BuildingInstance};");
                        }

                        factory.AppendLine(BlockFinish);
                        var builderLambdaExpression = (LambdaExpressionSyntax)SyntaxFactory.ParseExpression(factory.ToString());

                        metadataVisitor.VisitContract(new MdContract(semanticModel, invocation, buildersRootType, ContractKind.Explicit, ImmutableArray.Create(builderTag)));
                        VisitFactory(invocation, metadataVisitor, semanticModel, buildersRootType, builderLambdaExpression);

                        var builderRootName = GetName((SyntaxNode?)buildersArgs[0] ?? invocation, buildersName, buildersRootType) ?? Names.DefaultBuilderName;
                        var root = new MdRoot(invocation, semanticModel, buildersRootType, builderRootName, builderTag, buildersKind, invocationComments, true, builderRoots.ToImmutableArray());
                        metadataVisitor.VisitRoot(root);
                        break;

                    case nameof(IConfiguration.Builder):
                        if (genericName.TypeArgumentList.Arguments is not [{} builderRootTypeSyntax])
                        {
                            throw new CompileErrorException(
                                Strings.Error_InvalidBuilderType,
                                ImmutableArray.Create(locationProvider.GetLocation(invocation)),
                                LogId.ErrorInvalidMetadata);
                        }

                        var builderType = semantic.GetTypeSymbol<INamedTypeSymbol>(semanticModel, builderRootTypeSyntax);
                        var builderArgs = arguments.GetArgs(invocation.ArgumentList, "name", "kind");
                        var builderName = builderArgs[0] is {} builderNameArg ? GetName(builderNameArg, semantic.GetConstantValue<object>(semanticModel, builderNameArg.Expression, SmartTagKind.Name)?.ToString(), builderType) ?? Names.DefaultBuilderName : Names.DefaultBuilderName;
                        var builderKind = builderArgs[1] is {} builderKindArg ? semantic.GetConstantValue<RootKinds>(semanticModel, builderKindArg.Expression) : RootKinds.Default;
                        VisitBuilder(
                            invocation,
                            metadataVisitor,
                            semanticModel,
                            builderType,
                            builderName,
                            builderKind,
                            invocationComments);

                        break;

                    case nameof(IConfiguration.GenericTypeArgument):
                        if (TryGetAttributeType(genericName, semanticModel, out var genericTypeArgumentType))
                        {
                            var attr = new MdGenericTypeArgument(
                                semanticModel,
                                invocation.ArgumentList,
                                genericTypeArgumentType);

                            metadataVisitor.VisitGenericTypeArgument(attr);
                        }

                        break;

                    case nameof(IConfiguration.GenericTypeArgumentAttribute):
                        if (TryGetAttributeType(genericName, semanticModel, out var genericTypeArgumentAttributeType))
                        {
                            var attr = new MdGenericTypeArgumentAttribute(
                                semanticModel,
                                invocation,
                                genericTypeArgumentAttributeType);
                            metadataVisitor.VisitGenericTypeArgumentAttribute(attr);
                        }

                        break;

                    case nameof(IConfiguration.TypeAttribute):
                        if (TryGetAttributeType(genericName, semanticModel, out var typeAttributeType))
                        {
                            var attr = new MdTypeAttribute(
                                semanticModel,
                                invocation,
                                typeAttributeType,
                                BuildConstantArgs<object>(semanticModel, invocation.ArgumentList.Arguments) is [int positionVal] ? positionVal : 0);
                            metadataVisitor.VisitTypeAttribute(attr);
                        }

                        break;

                    case nameof(IConfiguration.TagAttribute):
                        if (TryGetAttributeType(genericName, semanticModel, out var tagAttributeType))
                        {
                            var attr = new MdTagAttribute(
                                semanticModel,
                                invocation,
                                tagAttributeType,
                                BuildConstantArgs<object>(semanticModel, invocation.ArgumentList.Arguments) is [int positionVal] ? positionVal : 0);
                            metadataVisitor.VisitTagAttribute(attr);
                        }

                        break;

                    case nameof(IConfiguration.OrdinalAttribute):
                        if (TryGetAttributeType(genericName, semanticModel, out var ordinalAttributeType))
                        {
                            var attr = new MdOrdinalAttribute(
                                semanticModel,
                                invocation,
                                ordinalAttributeType,
                                BuildConstantArgs<object>(semanticModel, invocation.ArgumentList.Arguments) is [int positionVal] ? positionVal : 0);
                            metadataVisitor.VisitOrdinalAttribute(attr);
                        }

                        break;

                    case nameof(IConfiguration.Accumulate):
                        if (genericName.TypeArgumentList.Arguments is [var typeSyntax, var accumulatorTypeSyntax])
                        {
                            var lifetimes = invocation.ArgumentList.Arguments
                                .SelectMany(i => semantic.GetConstantValues<Lifetime>(semanticModel, i.Expression))
                                .Distinct()
                                .OrderBy(i => i)
                                .ToList();

                            if (lifetimes.Count == 0)
                            {
                                lifetimes.AddRange(Enum.GetValues(typeof(Lifetime)).Cast<Lifetime>());
                            }

                            var typeSymbol = semantic.GetTypeSymbol<INamedTypeSymbol>(semanticModel, typeSyntax);
                            var accumulatorTypeSymbol = semantic.GetTypeSymbol<INamedTypeSymbol>(semanticModel, accumulatorTypeSyntax);
                            foreach (var lifetime in lifetimes)
                            {
                                metadataVisitor.VisitAccumulator(new MdAccumulator(semanticModel, invocation, typeSymbol, accumulatorTypeSymbol, lifetime));
                            }
                        }

                        break;

                    case nameof(IConfiguration.DefaultLifetime):
                        if (invocation.ArgumentList.Arguments.Count > 0
                            && genericName.TypeArgumentList.Arguments is [var defaultLifetimeTypeSyntax])
                        {
                            var defaultLifetimeTypeSymbol = semantic.GetTypeSymbol<ITypeSymbol>(semanticModel, defaultLifetimeTypeSyntax);
                            var defaultLifetimeArgs = arguments.GetArgs(invocation.ArgumentList, "lifetime", "tags");
                            var defaultLifetimeSyntax = defaultLifetimeArgs[0]!.Expression;
                            var defaultLifetimeTagArguments = invocation.ArgumentList.Arguments.SkipWhile((arg, i) => arg.NameColon?.Name.Identifier.Text != "tags" && i < 2);
                            var defaultLifetimeTags = BuildTags(semanticModel, defaultLifetimeTagArguments);
                            metadataVisitor.VisitDefaultLifetime(
                                new MdDefaultLifetime(
                                    new MdLifetime(semanticModel, invocation, semantic.GetRequiredConstantValue<Lifetime>(semanticModel, defaultLifetimeSyntax)),
                                    defaultLifetimeTypeSymbol,
                                    defaultLifetimeTags));
                        }

                        break;
                }

                break;
        }
    }

    private IEnumerable<INamedTypeSymbol> GetRelatedTypes(
        SyntaxNode source,
        SemanticModel semanticModel,
        InvocationExpressionSyntax invocation,
        INamedTypeSymbol baseType,
        string wildcardFilter)
    {
        return
            from type in semanticModel.LookupNamespacesAndTypes(invocation.Span.Start)
            where !type.IsAbstract
            let namedTypeSymbol = type as INamedTypeSymbol
            where namedTypeSymbol != null
            where !types.TypeEquals(baseType.ConstructedFrom, namedTypeSymbol.ConstructedFrom)
            let typeInfo = baseSymbolsProvider
                .GetBaseSymbols(namedTypeSymbol, (t, _) => t is INamedTypeSymbol)
                .FirstOrDefault(info => types.TypeEquals(baseType.ConstructedFrom, ((INamedTypeSymbol)info.Type).ConstructedFrom))
            where typeInfo != null
            where wildcardMatcher.Match(wildcardFilter.AsSpan(), symbolNames.GetName(namedTypeSymbol).AsSpan())
            orderby typeInfo.Deepness descending
            select CreateBuilderType(source, baseType, namedTypeSymbol);
    }

    private INamedTypeSymbol CreateBuilderType(SyntaxNode source, INamedTypeSymbol baseType, INamedTypeSymbol typeSymbol)
    {
        if (!typeSymbol.IsGenericType || !baseType.IsGenericType)
        {
            return typeSymbol;
        }

        var typesMap = baseType.TypeParameters.Zip(baseType.TypeArguments, (key, value) => (key: key.Name, value))
            .GroupBy(i => i.key)
            .Select(i => i.First())
            .ToDictionary(i => i.key, i => i.value);

        var markers = new List<ITypeSymbol>();
        foreach (var typeParameter in typeSymbol.TypeParameters)
        {
            if (typesMap.TryGetValue(typeParameter.Name, out var marker))
            {
                markers.Add(marker);
            }
            else
            {
                throw new CompileErrorException(
                    Strings.Error_TooManyTypeParameters,
                    ImmutableArray.Create(locationProvider.GetLocation(source)),
                    LogId.ErrorInvalidMetadata);
            }
        }

        return typeSymbol.Construct(markers.ToArray());
    }

    private MdRoot VisitBuilder(
        InvocationExpressionSyntax source,
        IMetadataVisitor metadataVisitor,
        SemanticModel semanticModel,
        INamedTypeSymbol builderType,
        string builderName,
        RootKinds kind,
        List<string> invocationComments)
    {
        var id = idGenerator.Generate();
        var builderArgTag = new MdTag(0, id + "BuilderArg" + Names.Salt);
        var builderTag = new MdTag(0, id + "Builder" + Names.Salt);

        // RootArg
        metadataVisitor.VisitContract(new MdContract(semanticModel, source, builderType, ContractKind.Explicit, ImmutableArray.Create(builderArgTag)));
        metadataVisitor.VisitArg(new MdArg(semanticModel, source, builderType, Names.BuildingInstance, ArgKind.Root, true, ["Instance for the build-up."]));

        // Factory
        var factory = new LinesBuilder();
        factory.AppendLine($"({Names.IContextTypeName} {Names.ContextInstance}) =>");
        factory.AppendLine(BlockStart);
        using (factory.Indent())
        {
            factory.AppendLine($"{Names.ContextInstance}.{nameof(IContext.Inject)}({builderArgTag.Value.ValueToString()}, out {symbolNames.GetName(builderType)} {Names.BuildingInstance});");
            factory.AppendLine($"{Names.ContextInstance}.{nameof(IContext.BuildUp)}({Names.BuildingInstance});");
            factory.AppendLine($"return {Names.BuildingInstance};");
        }

        factory.AppendLine(BlockFinish);
        var builderLambdaExpression = (LambdaExpressionSyntax)SyntaxFactory.ParseExpression(factory.ToString());

        metadataVisitor.VisitContract(new MdContract(semanticModel, source, builderType, ContractKind.Explicit, ImmutableArray.Create(builderTag)));
        VisitFactory(source, metadataVisitor, semanticModel, builderType, builderLambdaExpression);

        // Root
        var root = new MdRoot(source, semanticModel, builderType, builderName, builderTag, kind, invocationComments, true);
        metadataVisitor.VisitRoot(root);
        return root;
    }

    private void VisitSimpleFactory(
        IMetadataVisitor metadataVisitor,
        SemanticModel semanticModel,
        InvocationExpressionSyntax source,
        ITypeSymbol returnType,
        List<TypeSyntax> argsTypes,
        LambdaExpressionSyntax lambdaExpression)
    {
        CheckNotAsync(lambdaExpression);
        var parameters = new List<ParameterSyntax>();
        switch (lambdaExpression)
        {
            case ParenthesizedLambdaExpressionSyntax parenthesizedLambda:
                parameters.AddRange(parenthesizedLambda.ParameterList.Parameters);
                break;

            case SimpleLambdaExpressionSyntax simpleLambda:
                parameters.Add(simpleLambda.Parameter);
                break;
        }

        var paramAttributes = parameters.Select(i => i.AttributeLists.SelectMany(j => j.Attributes).ToList()).ToList();
        var resolvers = new List<MdResolver>();
        var namespaces = new List<string>();
        for (var i = 0; i < argsTypes.Count; i++)
        {
            var argTypeSyntax = argsTypes[i];
            var argType = semantic.GetTypeSymbol<ITypeSymbol>(semanticModel, argTypeSyntax);
            var argNamespace = argType.ContainingNamespace;
            if (semantic.IsValidNamespace(argNamespace))
            {
                namespaces.Add(argNamespace.ToString());
            }

            var attributes = paramAttributes[i];
            resolvers.Add(new MdResolver
            {
                SemanticModel = semanticModel,
                Source = source,
                ContractType = argType,
                Tag = new MdTag(0, null),
                ArgumentType = argTypeSyntax,
                Parameter = parameters[i],
                Position = i,
                Attributes = attributes.ToImmutableArray()
            });
        }

        metadataVisitor.VisitFactory(
            new MdFactory(
                semanticModel,
                source,
                returnType,
                localVariableRenamingRewriterFactory(),
                lambdaExpression,
                true,
                SyntaxFactory.Parameter(SyntaxFactory.Identifier("ctx_1182D127")),
                resolvers.ToImmutableArray(),
                ImmutableArray<MdInitializer>.Empty,
                false));

        metadataVisitor.VisitUsingDirectives(new MdUsingDirectives(namespaces, [], []));
    }

    private bool TryGetAttributeType(
        GenericNameSyntax genericName,
        SemanticModel semanticModel,
        [NotNullWhen(true)] out INamedTypeSymbol? type)
    {
        if (genericName.TypeArgumentList.Arguments is not [{} attributeTypeSyntax])
        {
            type = null;
            return false;
        }

        type = semantic.GetTypeSymbol<INamedTypeSymbol>(semanticModel, attributeTypeSyntax);
        if (type.IsGenericType)
        {
            type = type.ConstructUnboundGenericType();
        }

        return true;
    }

    private void VisitRoot(
        InvocationExpressionSyntax source,
        IMetadataVisitor metadataVisitor,
        SemanticModel semanticModel,
        InvocationExpressionSyntax invocation,
        IReadOnlyCollection<string> invocationComments,
        INamedTypeSymbol rootSymbol)
    {
        var rootArgs = arguments.GetArgs(invocation.ArgumentList, "name", "tag", "kind");
        var tag = rootArgs[1] is {} tagArg ? semantic.GetConstantValue<object>(semanticModel, tagArg.Expression, SmartTagKind.Tag) : null;
        var name = rootArgs[0] is {} nameArg
            ? GetName(nameArg, semantic.GetConstantValue<object>(semanticModel, nameArg.Expression, SmartTagKind.Name)?.ToString(), rootSymbol, tag) ?? ""
            : "";
        var kind = rootArgs[2] is {} kindArg ? semantic.GetConstantValue<RootKinds>(semanticModel, kindArg.Expression) : RootKinds.Default;
        metadataVisitor.VisitRoot(new MdRoot(source, semanticModel, rootSymbol, name, new MdTag(0, tag), kind, invocationComments, false));
    }

    private void VisitRoot(
        InvocationExpressionSyntax source,
        MdTag? tag,
        IMetadataVisitor metadataVisitor,
        SemanticModel semanticModel,
        InvocationExpressionSyntax invocation,
        IReadOnlyCollection<string> invocationComments,
        INamedTypeSymbol rootSymbol)
    {
        tag ??= new MdTag(0, null);
        var rootArgs = arguments.GetArgs(invocation.ArgumentList, "name", "kind");
        var name = rootArgs[0] is {} nameArg
            ? GetName(nameArg, semantic.GetConstantValue<object>(semanticModel, nameArg.Expression, SmartTagKind.Name)?.ToString(), rootSymbol, tag.Value.Value) ?? ""
            : "";
        var kind = rootArgs[1] is {} kindArg ? semantic.GetConstantValue<RootKinds>(semanticModel, kindArg.Expression) : RootKinds.Default;
        metadataVisitor.VisitRoot(new MdRoot(source, semanticModel, rootSymbol, name, tag, kind, invocationComments, false));
    }

    private void VisitBind(
        IMetadataVisitor metadataVisitor,
        SemanticModel semanticModel,
        InvocationExpressionSyntax invocation,
        GenericNameSyntax genericName)
    {
        var tags = BuildTags(semanticModel, invocation.ArgumentList.Arguments);
        VisitBind(metadataVisitor, semanticModel, invocation, tags, genericName);
    }

    private void VisitBind(
        IMetadataVisitor metadataVisitor,
        SemanticModel semanticModel,
        InvocationExpressionSyntax invocation,
        ImmutableArray<MdTag> tags,
        GenericNameSyntax genericName)
    {
        var contractTypes = genericName.TypeArgumentList.Arguments;
        foreach (var contractType in contractTypes)
        {
            metadataVisitor.VisitContract(
                new MdContract(
                    semanticModel,
                    invocation,
                    semantic.GetTypeSymbol<ITypeSymbol>(semanticModel, contractType),
                    ContractKind.Explicit,
                    tags));
        }
    }

    private void VisitArg(
        InvocationExpressionSyntax source,
        IMetadataVisitor metadataVisitor,
        SemanticModel semanticModel,
        ArgKind kind,
        InvocationExpressionSyntax invocation,
        GenericNameSyntax genericName,
        IReadOnlyCollection<string> argComments)
    {
        // ReSharper disable once InvertIf
        if (genericName.TypeArgumentList.Arguments is [{} argTypeSyntax]
            && invocation.ArgumentList.Arguments is [{ Expression: {} nameArgExpression }, ..] args)
        {
            var argType = semantic.GetTypeSymbol<INamedTypeSymbol>(semanticModel, argTypeSyntax);
            var tags = BuildTags(semanticModel, args.Skip(1));
            var name = GetName(
                nameArgExpression,
                semantic.GetRequiredConstantValue<object>(semanticModel, nameArgExpression, SmartTagKind.Name).ToString(),
                argType,
                tags.IsEmpty ? null : tags[0].Value) ?? "";

            metadataVisitor.VisitContract(new MdContract(semanticModel, source, argType, ContractKind.Explicit, tags.ToImmutableArray()));
            metadataVisitor.VisitArg(new MdArg(semanticModel, source, argType, name, kind, false, argComments));
        }
    }

    private void VisitFactory(
        InvocationExpressionSyntax source,
        IMetadataVisitor metadataVisitor,
        SemanticModel semanticModel,
        ITypeSymbol resultType,
        LambdaExpressionSyntax lambdaExpression)
    {
        CheckNotAsync(lambdaExpression);
        ParameterSyntax contextParameter;
        switch (lambdaExpression)
        {
            case SimpleLambdaExpressionSyntax simpleLambda:
                contextParameter = simpleLambda.Parameter;
                break;

            case ParenthesizedLambdaExpressionSyntax { ParameterList.Parameters: [{} singleParameter] }:
                contextParameter = singleParameter;
                break;

            default:
                return;
        }

        var localVariableRenamingRewriter = localVariableRenamingRewriterFactory()!;
        var factoryApiWalker = factoryApiWalkerFactory();
        factoryApiWalker.Visit(lambdaExpression);
        var resolversHasContextTag = false;
        var resolvers = factoryApiWalker.Meta
            .Where(i => i.Kind == FactoryMetaKind.Resolver)
            .Select(meta => CreateResolver(semanticModel, resultType, meta, contextParameter, ref resolversHasContextTag, localVariableRenamingRewriter))
            .Where(i => i != default)
            .ToImmutableArray();

        var initializersHasContextTag = false;
        var initializers = factoryApiWalker.Meta
            .Where(i => i.Kind == FactoryMetaKind.Initializer)
            .Select(meta => CreateInitializer(semanticModel, resultType, meta, contextParameter, ref initializersHasContextTag, localVariableRenamingRewriter))
            .Where(i => i != default)
            .ToImmutableArray();

        metadataVisitor.VisitFactory(
            new MdFactory(
                semanticModel,
                source,
                resultType,
                localVariableRenamingRewriter,
                lambdaExpression,
                false,
                contextParameter,
                resolvers,
                initializers,
                resolversHasContextTag || initializersHasContextTag));
    }

    private MdOverride CreateOverride(
        SemanticModel semanticModel,
        OverrideMeta @override,
        ParameterSyntax contextParameter,
        ILocalVariableRenamingRewriter localVariableRenamingRewriter,
        out bool hasContextTag)
    {
        hasContextTag = false;
        var invocation = @override.Expression;
        if (invocation.ArgumentList.Arguments.Count == 0)
        {
            return default;
        }

        var args = arguments.GetArgs(invocation.ArgumentList, "value", "tags");
        if (args[0] is not {} atgSyntax)
        {
            return default;
        }

        var argType = GetDefaultType(semanticModel, invocation, 0) ?? GetArgSymbol(semanticModel, atgSyntax);
        if (argType is null or IErrorTypeSymbol)
        {
            throw new CompileErrorException(
                Strings.Error_TypeCannotBeInferred,
                ImmutableArray.Create(locationProvider.GetLocation(atgSyntax)),
                LogId.ErrorTypeCannotBeInferred);
        }
        var tagArguments = invocation.ArgumentList.Arguments.Skip(1).ToList();
        var hasCtx = tagArguments.Aggregate(false, (current, tag) => current | HasContextTag(tag.Expression, contextParameter));
        var tags = BuildTags(semanticModel, tagArguments)
            .AsEnumerable()
            .Select(tag => tag with { Value = tag.Value ?? (hasCtx ? MdTag.ContextTag : null) })
            .DefaultIfEmpty(new MdTag(0, hasCtx ? MdTag.ContextTag : null))
            .ToImmutableArray();

        hasContextTag = hasCtx;
        ExpressionSyntax valueExpression;
        // ReSharper disable once ConvertSwitchStatementToSwitchExpression
        switch (semanticModel.GetOperation(atgSyntax.Expression))
        {
            case IMemberReferenceOperation:
            case IInvocationOperation:
                valueExpression = atgSyntax.Expression;
                break;

            default:
                valueExpression = (ExpressionSyntax)localVariableRenamingRewriter.Rewrite(semanticModel, false, true, atgSyntax.Expression);
                break;
        }

        return new MdOverride(
            semanticModel,
            invocation,
            overrideIdProvider.GetId(argType, tags),
            @override.Position,
            argType,
            tags,
            valueExpression);
    }

    private MdInitializer CreateInitializer(
        SemanticModel semanticModel,
        ITypeSymbol resultType,
        FactoryMeta meta,
        ParameterSyntax contextParameter,
        ref bool hasContextTag,
        ILocalVariableRenamingRewriter localVariableRenamingRewriter)
    {
        var invocation = meta.Expression;
        if (invocation.ArgumentList.Arguments is not [{} targetArg])
        {
            return default;
        }

        var targetType = GetArgSymbol(semanticModel, targetArg) ?? resultType;
        var overrides = new List<MdOverride>();
        // ReSharper disable once LoopCanBeConvertedToQuery
        foreach (var @override in meta.Overrides)
        {
            var mdOverride = CreateOverride(semanticModel, @override, contextParameter, localVariableRenamingRewriter, out hasContextTag);
            if (mdOverride != default)
            {
                overrides.Add(mdOverride);
            }
        }

        return new MdInitializer(
            semanticModel,
            invocation,
            meta.Position,
            targetType,
            targetArg.Expression,
            overrides.ToImmutableArray());
    }

    private MdResolver CreateResolver(
        SemanticModel semanticModel,
        ITypeSymbol resultType,
        FactoryMeta meta,
        ParameterSyntax contextParameter,
        ref bool hasContextTag,
        ILocalVariableRenamingRewriter localVariableRenamingRewriter)
    {
        var invocation = meta.Expression;
        if (invocation.ArgumentList.Arguments is not { Count: > 0 } invArguments)
        {
            return default;
        }

        var overrides = new List<MdOverride>();
        // ReSharper disable once LoopCanBeConvertedToQuery
        foreach (var overrideInvocation in meta.Overrides)
        {
            var mdOverride = CreateOverride(semanticModel, overrideInvocation, contextParameter, localVariableRenamingRewriter, out hasContextTag);
            if (mdOverride != default)
            {
                overrides.Add(mdOverride);
            }
        }

        switch (invArguments)
        {
            case [{ RefOrOutKeyword.IsMissing: false } targetValue]:
                var argSyntax = invArguments[0];
                var argType = GetDefaultType(semanticModel, invocation, 0) ?? GetArgSymbol(semanticModel, argSyntax);
                if (argType is null or IErrorTypeSymbol)
                {
                    throw new CompileErrorException(
                        Strings.Error_TypeCannotBeInferred,
                        ImmutableArray.Create(locationProvider.GetLocation(argSyntax)),
                        LogId.ErrorTypeCannotBeInferred);
                }

                return new MdResolver(
                    semanticModel,
                    invocation,
                    meta.Position,
                    argType,
                    null,
                    targetValue.Expression,
                    overrides.ToImmutableArray());

            default:
                var args = arguments.GetArgs(invocation.ArgumentList, "tag", "value");
                var tag = args[0]?.Expression;
                var hasCtx = HasContextTag(tag, contextParameter);
                hasContextTag |= hasCtx;
                var tagValue = hasCtx ? MdTag.ContextTag : tag is null ? null : semantic.GetConstantValue<object>(semanticModel, tag, SmartTagKind.Tag);
                var resolverTag = new MdTag(0, tagValue);
                if (args[1] is {} argSyntax2)
                {
                    var argType2 = GetDefaultType(semanticModel, invocation, 1) ?? GetArgSymbol(semanticModel, argSyntax2) ?? resultType;
                    if (argType2 is null or IErrorTypeSymbol)
                    {
                        throw new CompileErrorException(
                            Strings.Error_TypeCannotBeInferred,
                            ImmutableArray.Create(locationProvider.GetLocation(argSyntax2)),
                            LogId.ErrorTypeCannotBeInferred);
                    }

                    return new MdResolver(
                        semanticModel,
                        invocation,
                        meta.Position,
                        argType2,
                        resolverTag,
                        argSyntax2.Expression,
                        overrides.ToImmutableArray());
                }

                break;
        }

        return default;
    }

    private ITypeSymbol? GetDefaultType(SemanticModel semanticModel, InvocationExpressionSyntax invocation, int typeArgPosition)
    {
        var name = invocation.Expression switch
        {
            MemberAccessExpressionSyntax memberAccess when memberAccess.Kind() == SyntaxKind.SimpleMemberAccessExpression => memberAccess.Name,
            _ => null
        };

        ITypeSymbol? defaultType = null;
        if (name is GenericNameSyntax genericName && genericName.TypeArgumentList.Arguments.Count > typeArgPosition)
        {
            defaultType = semantic.GetTypeSymbol<ITypeSymbol>(semanticModel, genericName.TypeArgumentList.Arguments[typeArgPosition]);
        }

        return defaultType;
    }

    private static bool HasContextTag(ExpressionSyntax? tag, ParameterSyntax contextParameter) =>
        tag is MemberAccessExpressionSyntax memberAccessExpression
        && memberAccessExpression.IsKind(SyntaxKind.SimpleMemberAccessExpression)
        && memberAccessExpression.Name.Identifier.Text == nameof(IContext.Tag)
        && memberAccessExpression.Expression is IdentifierNameSyntax identifierName
        && identifierName.Identifier.Text == contextParameter.Identifier.Text;

    private static ITypeSymbol? GetArgSymbol(SemanticModel semanticModel, ArgumentSyntax argumentSyntax)
    {
        if (argumentSyntax.SyntaxTree != semanticModel.SyntaxTree)
        {
            var typeSyntax = argumentSyntax.Expression switch
            {
                DeclarationExpressionSyntax declarationExpressionSyntax => declarationExpressionSyntax.Type,
                _ => null
            };

            return typeSyntax is not null ? semanticModel.Compilation.GetTypeByMetadataName(typeSyntax.ToString()) : null;
        }

        if (semanticModel.GetOperation(argumentSyntax) is IArgumentOperation { Value.Type: {} valueType })
        {
            return valueType;
        }

        // ReSharper disable once ConvertIfStatementToReturnStatement
        if (semanticModel.GetOperation(argumentSyntax.Expression) is { Type: {} declarationType })
        {
            return declarationType;
        }

        return null;
    }

    private void CheckNotAsync(LambdaExpressionSyntax lambdaExpression)
    {
        if (lambdaExpression.AsyncKeyword.IsKind(SyntaxKind.AsyncKeyword))
        {
            throw new CompileErrorException(
                Strings.Error_AsynchronousFactoryWithAsyncNotSupported,
                ImmutableArray.Create(locationProvider.GetLocation(lambdaExpression.AsyncKeyword)),
                LogId.ErrorInvalidMetadata);
        }
    }

    // ReSharper disable once SuggestBaseTypeForParameter
    private void NotSupported(SyntaxNode source) =>
        throw new CompileErrorException(
            string.Format(Strings.Error_Template_NotSupported, source),
            ImmutableArray.Create(locationProvider.GetLocation(source)),
            LogId.ErrorInvalidMetadata);

    private IReadOnlyList<T> BuildConstantArgs<T>(
        SemanticModel semanticModel,
        SeparatedSyntaxList<ArgumentSyntax> args) =>
        args
            .SelectMany(a => semantic.GetConstantValues<T>(semanticModel, a.Expression).Select(value => (value, a.Expression)))
            .Select(a => a.value ?? throw new CompileErrorException(
                string.Format(Strings.Error_Template_MustBeValueOfType, a.Expression, typeof(T)),
                ImmutableArray.Create(locationProvider.GetLocation(a.Expression)),
                LogId.ErrorInvalidMetadata))
            .ToList();

    private ImmutableArray<MdTag> BuildTags(
        SemanticModel semanticModel,
        IEnumerable<ArgumentSyntax> args) =>
        args
            .SelectMany(t => semantic.GetConstantValues<object>(semanticModel, t.Expression, SmartTagKind.Tag))
            .Select((tag, i) => new MdTag(i, tag))
            .ToImmutableArray();

    private static CompositionName CreateCompositionName(
        string name,
        string ns,
        SyntaxNode source)
    {
        string className;
        string newNamespace;
        if (!string.IsNullOrWhiteSpace(name))
        {
            var compositionTypeNameParts = name.Split(TypeNamePartsSeparators, StringSplitOptions.RemoveEmptyEntries);
            className = compositionTypeNameParts.Last();
            newNamespace = string.Join(".", compositionTypeNameParts.Take(compositionTypeNameParts.Length - 1)).Trim();
        }
        else
        {
            className = "";
            newNamespace = "";
        }

        if (string.IsNullOrWhiteSpace(newNamespace))
        {
            newNamespace = ns;
        }

        return new CompositionName(className, newNamespace, source);
    }

    private string? GetName(
        SyntaxNode source,
        string? nameTemplate,
        INamedTypeSymbol? type = null,
        object? tag = null)
    {
        if (string.IsNullOrWhiteSpace(nameTemplate))
        {
            return nameTemplate;
        }

        var name = nameFormatter.Format(nameTemplate!, type?.OriginalDefinition, tag);
        if (!SyntaxFacts.IsValidIdentifier(name))
        {
            throw new CompileErrorException(
                string.Format(Strings.Error_Template_InvalidIdentifier, name),
                ImmutableArray.Create(locationProvider.GetLocation(source)),
                LogId.ErrorInvalidMetadata);
        }

        return name;
    }
}