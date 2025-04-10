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
    ITypes types,
    IWildcardMatcher wildcardMatcher,
    Func<INamespacesWalker> namespacesWalkerFactory,
    Func<IFactoryApiWalker> factoryApiWalkerFactory,
    Func<ILocalVariableRenamingRewriter> localVariableRenamingRewriterFactory)
    : IApiInvocationProcessor
{
    private static readonly char[] TypeNamePartsSeparators = ['.'];
    private static readonly object NullTag = new();

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
                                invocation.ArgumentList,
                                null,
                                ContractKind.Implicit,
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
                                        VisitFactory(metadataVisitor, semanticModel, resultType, lambdaExpression);
                                        break;
                                    }
                                }

                                VisitFactory(metadataVisitor, semanticModel, type, lambdaExpression);
                                break;

                            case []:
                                break;

                            default:
                                NotSupported(name);
                                break;
                        }

                        break;

                    case nameof(IBinding.As):
                        if (invocation.ArgumentList.Arguments is [{ Expression: {} lifetimeExpression }])
                        {
                            metadataVisitor.VisitLifetime(new MdLifetime(semanticModel, invocation.ArgumentList, semantic.GetRequiredConstantValue<Lifetime>(semanticModel, lifetimeExpression)));
                        }

                        break;

                    case nameof(IConfiguration.Hint):
                        var hintArgs = arguments.GetArgs(invocation.ArgumentList, "hint", "value");
                        if (hintArgs is [{ Expression: {} hintNameExpression }, { Expression: {} hintValueExpression }])
                        {
                            var values = new LinkedList<string>();
                            values.AddLast(semantic.GetRequiredConstantValue<string>(semanticModel, hintValueExpression));
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
                                name,
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
                                Array.Empty<MdTagOnSites>(),
                                comments.FilterHints(invocationComments).ToList()));
                        break;

                    case nameof(IConfiguration.DefaultLifetime):
                        if (invocation.ArgumentList.Arguments is [{ Expression: {} defaultLifetimeSyntax }])
                        {
                            metadataVisitor.VisitDefaultLifetime(new MdDefaultLifetime(new MdLifetime(semanticModel, invocation.ArgumentList, semantic.GetRequiredConstantValue<Lifetime>(semanticModel, defaultLifetimeSyntax))));
                        }

                        break;

                    case nameof(IConfiguration.DependsOn):
                        if (BuildConstantArgs<string>(semanticModel, invocation.ArgumentList.Arguments) is [..] compositionTypeNames)
                        {
                            metadataVisitor.VisitDependsOn(new MdDependsOn(semanticModel, invocation.ArgumentList, compositionTypeNames.Select(i => CreateCompositionName(i, @namespace, invocation.ArgumentList)).ToImmutableArray()));
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
                            throw new CompileErrorException(Strings.Error_InvalidRootType, name.GetLocation(), LogId.ErrorInvalidMetadata);
                        }

                        var tagArguments = invocation.ArgumentList.Arguments.SkipWhile((arg, i) => arg.NameColon?.Name.Identifier.Text != "tags" && i < 2);
                        var tags = BuildTags(semanticModel, tagArguments);
                        VisitBind(metadataVisitor, semanticModel, invocation, tags, genericName);
                        var rootBindSymbol = semantic.GetTypeSymbol<INamedTypeSymbol>(semanticModel, rootBindType);
                        VisitRoot(name, tags.FirstOrDefault(), metadataVisitor, semanticModel, invocation, invocationComments, rootBindSymbol);
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
                                    VisitFactory(metadataVisitor, semanticModel, factoryType, lambdaExpression);
                                    break;

                                case [{ Expression: LiteralExpressionSyntax { Token.Value: string sourceCodeStatement } }]:
                                    var lambda = SyntaxFactory
                                        .SimpleLambdaExpression(SyntaxFactory.Parameter(SyntaxFactory.Identifier("_")))
                                        .WithExpressionBody(SyntaxFactory.IdentifierName(sourceCodeStatement));
                                    factoryType = semantic.GetTypeSymbol<ITypeSymbol>(semanticModel, implementationTypeSyntax);
                                    VisitFactory(metadataVisitor, semanticModel, factoryType, lambda, true);
                                    break;

                                case []:
                                    var implementationType = semantic.GetTypeSymbol<INamedTypeSymbol>(semanticModel, implementationTypeSyntax);
                                    metadataVisitor.VisitImplementation(new MdImplementation(semanticModel, implementationTypeSyntax, implementationType));
                                    break;

                                default:
                                    NotSupported(name);
                                    break;
                            }
                        }

                        break;

                    case nameof(IConfiguration.Arg):
                        VisitArg(metadataVisitor, semanticModel, ArgKind.Class, invocation, genericName, invocationComments);
                        break;

                    case nameof(IConfiguration.RootArg):
                        VisitArg(metadataVisitor, semanticModel, ArgKind.Root, invocation, genericName, invocationComments);
                        break;

                    case nameof(IConfiguration.Roots):
                        if (genericName.TypeArgumentList.Arguments is not [{} rootsTypeSyntax]
                            || semantic.TryGetTypeSymbol<INamedTypeSymbol>(semanticModel, rootsTypeSyntax) is not {} rootsType
                            // ReSharper disable once MergeIntoNegatedPattern
                            || rootsType.SpecialType == Microsoft.CodeAnalysis.SpecialType.System_Object)
                        {
                            throw new CompileErrorException(Strings.Error_InvalidRootsRype, name.GetLocation(), LogId.ErrorInvalidMetadata);
                        }

                        var rootsArgs = arguments.GetArgs(invocation.ArgumentList, "name", "kind", "filter");
                        var rootsName = rootsArgs[0] is {} rootsNameArg ? semantic.GetConstantValue<string>(semanticModel, rootsNameArg.Expression) ?? "" : "";
                        var rootsKind = rootsArgs[1] is {} rootsKindArg ? semantic.GetConstantValue<RootKinds>(semanticModel, rootsKindArg.Expression) : RootKinds.Default;
                        var rootsWildcardFilter = (rootsArgs[2] is {} rootsFilterArg ? semantic.GetConstantValue<string>(semanticModel, rootsFilterArg.Expression) : "*") ?? "*";
                        var hasRootsType = false;
                        foreach (var rootType in GetRelatedTypes(name, semanticModel, invocation, rootsType, rootsWildcardFilter))
                        {
                            var rootName = GetName((SyntaxNode?)rootsArgs[1] ?? invocation, rootsName, rootType) ?? "";
                            metadataVisitor.VisitRoot(new MdRoot(name, semanticModel, rootType, rootName, new MdTag(0, null), rootsKind, invocationComments, false));
                            hasRootsType = true;
                        }

                        if (!hasRootsType)
                        {
                            throw new CompileErrorException(string.Format(Strings.Error_Template_NoTypeForWildcard, symbolNames.GetName(rootsType), rootsWildcardFilter), name.GetLocation(), LogId.ErrorInvalidMetadata);
                        }

                        break;

                    case nameof(IConfiguration.Root):
                        if (genericName.TypeArgumentList.Arguments is not [{} rootTypeSyntax])
                        {
                            throw new CompileErrorException(Strings.Error_InvalidRootType, name.GetLocation(), LogId.ErrorInvalidMetadata);
                        }

                        var rootSymbol = semantic.GetTypeSymbol<INamedTypeSymbol>(semanticModel, rootTypeSyntax);
                        VisitRoot(name, metadataVisitor, semanticModel, invocation, invocationComments, rootSymbol);
                        break;

                    case nameof(IConfiguration.Builders):
                        if (genericName.TypeArgumentList.Arguments is not [{} buildersRootTypeSyntax]
                            || semantic.TryGetTypeSymbol<INamedTypeSymbol>(semanticModel, buildersRootTypeSyntax) is not {} buildersRootType
                            // ReSharper disable once MergeIntoNegatedPattern
                            || buildersRootType.SpecialType == Microsoft.CodeAnalysis.SpecialType.System_Object)
                        {
                            throw new CompileErrorException(Strings.Error_InvalidBuildersType, name.GetLocation(), LogId.ErrorInvalidMetadata);
                        }

                        var buildersArgs = arguments.GetArgs(invocation.ArgumentList, "name", "kind", "filter");
                        var buildersName = buildersArgs[0] is {} buildersNameArg ? semantic.GetConstantValue<string>(semanticModel, buildersNameArg.Expression) ?? Names.DefaultBuilderName : Names.DefaultBuilderName;
                        var buildersKind = buildersArgs[1] is {} buildersKindArg ? semantic.GetConstantValue<RootKinds>(semanticModel, buildersKindArg.Expression) : RootKinds.Default;
                        var buildersWildcardFilter = (buildersArgs[2] is {} buildersFilterArg ? semantic.GetConstantValue<string>(semanticModel, buildersFilterArg.Expression) : "*") ?? "*";
                        var hasBuildersType = false;
                        foreach (var buildersType in GetRelatedTypes(name, semanticModel, invocation, buildersRootType, buildersWildcardFilter))
                        {
                            var buildersItemName = GetName((SyntaxNode?)buildersArgs[0] ?? invocation, buildersName, buildersType) ?? Names.DefaultBuilderName;
                            VisitBuilder(
                                name,
                                metadataVisitor,
                                semanticModel,
                                buildersRootTypeSyntax,
                                buildersType,
                                buildersItemName,
                                buildersKind,
                                invocationComments);

                            hasBuildersType = true;
                        }

                        if (!hasBuildersType)
                        {
                            throw new CompileErrorException(string.Format(Strings.Error_Template_NoTypeForWildcard, symbolNames.GetName(buildersRootType), buildersWildcardFilter), name.GetLocation(), LogId.ErrorInvalidMetadata);
                        }

                        break;

                    case nameof(IConfiguration.Builder):
                        if (genericName.TypeArgumentList.Arguments is not [{} builderRootTypeSyntax])
                        {
                            throw new CompileErrorException(Strings.Error_InvalidBuilderType, name.GetLocation(), LogId.ErrorInvalidMetadata);
                        }

                        var builderType = semantic.GetTypeSymbol<INamedTypeSymbol>(semanticModel, builderRootTypeSyntax);
                        var builderArgs = arguments.GetArgs(invocation.ArgumentList, "name", "kind");
                        var builderName = builderArgs[0] is {} builderNameArg ? GetName(builderNameArg, semantic.GetConstantValue<string>(semanticModel, builderNameArg.Expression), builderType) ?? Names.DefaultBuilderName : Names.DefaultBuilderName;
                        var builderKind = builderArgs[1] is {} builderKindArg ? semantic.GetConstantValue<RootKinds>(semanticModel, builderKindArg.Expression) : RootKinds.Default;
                        VisitBuilder(
                            name,
                            metadataVisitor,
                            semanticModel,
                            builderRootTypeSyntax,
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
                                invocation.ArgumentList,
                                genericTypeArgumentAttributeType);
                            metadataVisitor.VisitGenericTypeArgumentAttribute(attr);
                        }

                        break;

                    case nameof(IConfiguration.TypeAttribute):
                        if (TryGetAttributeType(genericName, semanticModel, out var typeAttributeType))
                        {
                            var attr = new MdTypeAttribute(
                                semanticModel,
                                invocation.ArgumentList,
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
                                invocation.ArgumentList,
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
                                invocation.ArgumentList,
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
                                    new MdLifetime(semanticModel, invocation.ArgumentList, semantic.GetRequiredConstantValue<Lifetime>(semanticModel, defaultLifetimeSyntax)),
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
        if (baseType.IsGenericType)
        {
            baseType = baseType.ConstructUnboundGenericType();
        }

        return semanticModel
            .LookupNamespacesAndTypes(invocation.Span.Start)
            .OfType<INamedTypeSymbol>()
            .Where(type => !type.IsAbstract)
            .Where(type =>
                baseSymbolsProvider.GetBaseSymbols(type, (t, _) => t is INamedTypeSymbol)
                    .OfType<INamedTypeSymbol>()
                    .Select(typeSymbol => typeSymbol.IsGenericType ? typeSymbol.ConstructUnboundGenericType() : typeSymbol)
                    .Any(typeSymbol => types.TypeEquals(baseType, typeSymbol)))
            .Where(type => !types.TypeEquals(baseType, type))
            .Where(type => wildcardMatcher.Match(wildcardFilter.AsSpan(), symbolNames.GetName(type).AsSpan()))
            .Select(typeSymbol => {
                if (!typeSymbol.IsGenericType)
                {
                    return typeSymbol;
                }

                var typeArgsCount = typeSymbol.TypeArguments.Length;
                var markers = new List<ITypeSymbol>();
                for (var index = 0; index < typeArgsCount; index++)
                {
                    var marker = types.GetMarker(index, semanticModel.Compilation);
                    if (marker is null)
                    {
                        throw new CompileErrorException(Strings.Error_TooManyTypeParameters, source.GetLocation(), LogId.ErrorInvalidMetadata);
                    }

                    markers.Add(marker);
                }

                return typeSymbol.Construct(markers.ToArray());
            });
    }

    private void VisitBuilder(
        SyntaxNode source,
        IMetadataVisitor metadataVisitor,
        SemanticModel semanticModel,
        SyntaxNode typeSource,
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
        metadataVisitor.VisitArg(new MdArg(semanticModel, typeSource, builderType, Names.BuildingInstance, ArgKind.Root, true, ["Instance for the build-up."]));

        // Factory
        var factory = new StringBuilder();
        factory.AppendLine($"({Names.IContextTypeName} {Names.ContextInstance}) =>");
        factory.AppendLine(BlockStart);
        factory.AppendLine($"{Names.ContextInstance}.{nameof(IContext.Inject)}({builderArgTag.Value.ValueToString()}, out {symbolNames.GetName(builderType)} {Names.BuildingInstance});");
        factory.AppendLine($"{Names.ContextInstance}.{nameof(IContext.BuildUp)}({Names.BuildingInstance});");
        factory.AppendLine($"return {Names.BuildingInstance};");
        factory.AppendLine(BlockFinish);
        var builderLambdaExpression = (LambdaExpressionSyntax)SyntaxFactory.ParseExpression(factory.ToString());

        metadataVisitor.VisitContract(new MdContract(semanticModel, source, builderType, ContractKind.Explicit, ImmutableArray.Create(builderTag)));
        VisitFactory(metadataVisitor, semanticModel, builderType, builderLambdaExpression, true);

        // Root
        metadataVisitor.VisitRoot(new MdRoot(source, semanticModel, builderType, builderName, builderTag, kind, invocationComments, true));
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
        var namespaces = new HashSet<string>();
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
                Source = argTypeSyntax,
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

        metadataVisitor.VisitUsingDirectives(new MdUsingDirectives(namespaces.ToImmutableArray(), ImmutableArray<string>.Empty));
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
        SyntaxNode source,
        IMetadataVisitor metadataVisitor,
        SemanticModel semanticModel,
        InvocationExpressionSyntax invocation,
        IReadOnlyCollection<string> invocationComments,
        INamedTypeSymbol rootSymbol)
    {
        var rootArgs = arguments.GetArgs(invocation.ArgumentList, "name", "tag", "kind");
        var tag = rootArgs[1] is {} tagArg ? semantic.GetConstantValue<object>(semanticModel, tagArg.Expression) : null;
        var name = rootArgs[0] is {} nameArg
            ? GetName(nameArg, semantic.GetConstantValue<string>(semanticModel, nameArg.Expression), rootSymbol, tag) ?? ""
            : "";
        var kind = rootArgs[2] is {} kindArg ? semantic.GetConstantValue<RootKinds>(semanticModel, kindArg.Expression) : RootKinds.Default;
        metadataVisitor.VisitRoot(new MdRoot(source, semanticModel, rootSymbol, name, new MdTag(0, tag), kind, invocationComments, false));
    }

    private void VisitRoot(
        SyntaxNode source,
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
            ? GetName(nameArg, semantic.GetConstantValue<string>(semanticModel, nameArg.Expression), rootSymbol, tag.Value.Value) ?? ""
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
                    invocation.ArgumentList,
                    semantic.GetTypeSymbol<ITypeSymbol>(semanticModel, contractType),
                    ContractKind.Explicit,
                    tags));
        }
    }

    private void VisitArg(
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
                semantic.GetRequiredConstantValue<string>(semanticModel, nameArgExpression),
                argType,
                tags.IsEmpty ? null : tags[0].Value) ?? "";

            metadataVisitor.VisitContract(new MdContract(semanticModel, genericName, argType, ContractKind.Explicit, tags.ToImmutableArray()));
            metadataVisitor.VisitArg(new MdArg(semanticModel, genericName, argType, name, kind, false, argComments));
        }
    }

    private void VisitFactory(
        IMetadataVisitor metadataVisitor,
        SemanticModel semanticModel,
        ITypeSymbol resultType,
        LambdaExpressionSyntax lambdaExpression,
        bool isManual = false)
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

        if (!isManual)
        {
            VisitUsingDirectives(metadataVisitor, semanticModel, lambdaExpression);
        }

        metadataVisitor.VisitFactory(
            new MdFactory(
                semanticModel,
                lambdaExpression,
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
        if (args[0] is not {} valueArg)
        {
            return default;
        }

        var argType = GetArg(semanticModel, GetDefaultType(semanticModel, invocation, 0), valueArg, invocation);
        if (argType is null)
        {
            return default;
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
        switch (semanticModel.GetOperation(valueArg.Expression))
        {
            case IMemberReferenceOperation:
            case IInvocationOperation:
                valueExpression = valueArg.Expression;
                break;

            default:
                valueExpression = (ExpressionSyntax)localVariableRenamingRewriter.Rewrite(semanticModel, false, true, valueArg.Expression);
                break;
        }

        return new MdOverride(
            semanticModel,
            invocation,
            overrideIdProvider.GetId(argType, tags.Select(tag => tag.Value ?? NullTag).ToImmutableHashSet()),
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

        var targetType = GetArgSymbol(semanticModel, targetArg, resultType);
        if (targetType is null)
        {
            return default;
        }

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
                var argSymbol = GetArgSymbol(semanticModel, invArguments[0], GetDefaultType(semanticModel, invocation, 1)) ?? resultType;
                return new MdResolver(
                    semanticModel,
                    invocation,
                    meta.Position,
                    argSymbol,
                    null,
                    targetValue.Expression,
                    overrides.ToImmutableArray());

            default:
                var args = arguments.GetArgs(invocation.ArgumentList, "tag", "value");
                var tag = args[0]?.Expression;
                var hasCtx = HasContextTag(tag, contextParameter);
                hasContextTag |= hasCtx;
                var tagValue = hasCtx ? MdTag.ContextTag : tag is null ? null : semantic.GetConstantValue<object>(semanticModel, tag);
                var resolverTag = new MdTag(0, tagValue);
                if (args[1] is {} valueArg)
                {
                    var argType = GetArg(semanticModel, GetDefaultType(semanticModel, invocation, 1), valueArg, invocation) ?? resultType;
                    return new MdResolver(
                        semanticModel,
                        invocation,
                        meta.Position,
                        argType,
                        resolverTag,
                        valueArg.Expression,
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

    private static ITypeSymbol? GetArg(
        SemanticModel semanticModel,
        ITypeSymbol? resultType,
        ArgumentSyntax valueArg,
        InvocationExpressionSyntax invocation)
    {
        var argType = GetArgSymbol(semanticModel, valueArg, resultType);
        if (argType is null
            && invocation.SyntaxTree == semanticModel.SyntaxTree && semanticModel.GetOperation(invocation) is {} invocationOperation
            && invocationOperation.ChildOperations.OfType<IDeclarationExpressionOperation>().FirstOrDefault() is { Type: {} declarationType })
        {
            argType = declarationType;
        }

        return argType;
    }

    private static bool HasContextTag(ExpressionSyntax? tag, ParameterSyntax contextParameter) =>
        tag is MemberAccessExpressionSyntax memberAccessExpression
        && memberAccessExpression.IsKind(SyntaxKind.SimpleMemberAccessExpression)
        && memberAccessExpression.Name.Identifier.Text == nameof(IContext.Tag)
        && memberAccessExpression.Expression is IdentifierNameSyntax identifierName
        && identifierName.Identifier.Text == contextParameter.Identifier.Text;

    private static ITypeSymbol? GetArgSymbol(SemanticModel semanticModel, ArgumentSyntax argumentSyntax, ITypeSymbol? defaultType)
    {
        ITypeSymbol? argType = null;
        if (argumentSyntax.SyntaxTree != semanticModel.SyntaxTree)
        {
            ExpressionSyntax? typeSyntax = null;
            switch (argumentSyntax.Expression)
            {
                case DeclarationExpressionSyntax declarationExpressionSyntax:
                    typeSyntax = declarationExpressionSyntax.Type;
                    break;

                default:
                    argType = defaultType;
                    break;
            }

            if (typeSyntax is not null)
            {
                argType = semanticModel.Compilation.GetTypeByMetadataName(typeSyntax.ToString()) ?? defaultType;
            }
        }

        if (argType is null
            && argumentSyntax.SyntaxTree == semanticModel.SyntaxTree
            && semanticModel.GetOperation(argumentSyntax) is IArgumentOperation { Value.Type: {} valueType })
        {
            argType = valueType;
        }

        return argType ?? defaultType;
    }

    private static void CheckNotAsync(LambdaExpressionSyntax lambdaExpression)
    {
        if (lambdaExpression.AsyncKeyword.IsKind(SyntaxKind.AsyncKeyword))
        {
            throw new CompileErrorException(Strings.Error_AsynchronousFactoryWithAsyncNotSupported, lambdaExpression.AsyncKeyword.GetLocation(), LogId.ErrorInvalidMetadata);
        }
    }

    private void VisitUsingDirectives(
        IMetadataVisitor metadataVisitor,
        SemanticModel semanticModel,
        SyntaxNode node)
    {
        var namespacesWalker = namespacesWalkerFactory().Initialize(semanticModel);
        namespacesWalker.Visit(node);
        var namespaces = namespacesWalker.GetResult();
        if (namespaces.Count > 0)
        {
            metadataVisitor.VisitUsingDirectives(new MdUsingDirectives(namespaces.ToImmutableArray(), ImmutableArray<string>.Empty));
        }
    }

    // ReSharper disable once SuggestBaseTypeForParameter
    private static void NotSupported(SyntaxNode source) =>
        throw new CompileErrorException(string.Format(Strings.Error_Template_NotSupported, source), source.GetLocation(), LogId.ErrorInvalidMetadata);

    private IReadOnlyList<T> BuildConstantArgs<T>(
        SemanticModel semanticModel,
        SeparatedSyntaxList<ArgumentSyntax> args) =>
        args
            .SelectMany(a => semantic.GetConstantValues<T>(semanticModel, a.Expression).Select(value => (value, a.Expression)))
            .Select(a => a.value ?? throw new CompileErrorException(
                string.Format(Strings.Error_Template_MustBeValueOfType, a.Expression, typeof(T)),
                a.Expression.GetLocation(),
                LogId.ErrorInvalidMetadata))
            .ToList();

    private ImmutableArray<MdTag> BuildTags(
        SemanticModel semanticModel,
        IEnumerable<ArgumentSyntax> args) =>
        args
            .SelectMany(t => semantic.GetConstantValues<object>(semanticModel, t.Expression))
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
            throw new CompileErrorException(string.Format(Strings.Error_Template_InvalidIdentifier, name), source.GetLocation(), LogId.ErrorInvalidMetadata);
        }

        return name;
    }
}