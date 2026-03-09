// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable ConvertIfStatementToConditionalTernaryExpression

namespace Pure.DI.Core;

using Microsoft.CodeAnalysis.Operations;

sealed class ApiInvocationProcessor(
    IComments comments,
    IArguments arguments,
    ISemantic semantic,
    ISymbolNames symbolNames,
    [Tag(Tag.UniqueTagIdGenerator)] IIdGenerator idGenerator,
    IOverrideIdProvider overrideIdProvider,
    IBaseSymbolsProvider baseSymbolsProvider,
    INameFormatter nameFormatter,
    ITypes types,
    IWildcardMatcher wildcardMatcher,
    Func<IFactoryApiWalker> factoryApiWalkerFactory,
    Func<ILocalVariableRenamingRewriter> localVariableRenamingRewriterFactory,
    ILocationProvider locationProvider,
    INameProvider nameProvider)
    : IApiInvocationProcessor
{
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
                                var type = semantic.TryGetTypeSymbol<ITypeSymbol>(semanticModel, lambdaExpression) ?? semantic.GetTypeSymbol<ITypeSymbol>(semanticModel, lambdaExpression.Body);
                                if (type is INamedTypeSymbol symbol)
                                {
                                    var typeArgumentsCount = symbol.TypeArguments.Length;
                                    if (typeArgumentsCount > 1
                                        && symbolNames.GetGlobalName(symbol.TypeArguments[0]) != Names.IContextTypeName)
                                    {
                                        switch (invocation.ArgumentList.Arguments[0].Expression)
                                        {
                                            // To((T1 arg1, T2 arg2) => ..)
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

                                    if (typeArgumentsCount is 1 or 2 && symbol.TypeArguments is [.., {} resultType])
                                    {
                                        // .To(ctx => ...)
                                        VisitFactory(invocation, metadataVisitor, semanticModel, resultType, lambdaExpression);
                                        break;
                                    }
                                }

                                VisitFactory(invocation, metadataVisitor, semanticModel, type, lambdaExpression);
                                break;

                            case [{ Expression: {} expression }]:
                                var expressionType = semantic.TryGetTypeSymbol<ITypeSymbol>(semanticModel, expression);
                                if (expressionType is INamedTypeSymbol { IsGenericType: true, TypeArguments.Length: 1 } expressionTypeSymbol
                                    && expressionTypeSymbol.TypeArguments is [{} expressionResultType])
                                {
                                    // To(Guid.NewGuid)
                                    VisitFactory(
                                        invocation,
                                        metadataVisitor,
                                        semanticModel,
                                        expressionResultType,
                                        SyntaxFactory.ParenthesizedLambdaExpression(
                                            SyntaxFactory.InvocationExpression(expression)));
                                }
                                else
                                {
                                    NotSupported(invocation);
                                }

                                break;

                            default:
                                NotSupported(invocation);
                                break;
                        }

                        break;

                    case nameof(IConfiguration.Transient):
                    case nameof(IConfiguration.Singleton):
                    case nameof(IConfiguration.Scoped):
                    case nameof(IConfiguration.PerResolve):
                    case nameof(IConfiguration.PerBlock):
                        if (!Enum.TryParse<Lifetime>(identifierName.Identifier.Text, out var lifetime))
                        {
                            NotSupported(invocation);
                            break;
                        }

                        switch (invocation.ArgumentList.Arguments)
                        {
                            case [{ Expression: LambdaExpressionSyntax lambdaExpression }, ..]:
                                var type = semantic.TryGetTypeSymbol<ITypeSymbol>(semanticModel, lambdaExpression)
                                           ?? semantic.GetTypeSymbol<ITypeSymbol>(semanticModel, lambdaExpression.Body);

                                if (type is INamedTypeSymbol symbol)
                                {
                                    var typeArgumentsCount = symbol.TypeArguments.Length;
                                    if (typeArgumentsCount > 1
                                        && symbolNames.GetGlobalName(symbol.TypeArguments[0]) != Names.IContextTypeName)
                                    {
                                        switch (invocation.ArgumentList.Arguments[0].Expression)
                                        {
                                            // .Transient((T1 arg1, T2 arg2) => .., tags)
                                            case ParenthesizedLambdaExpressionSyntax { ParameterList.Parameters.Count: > 0 } parenthesizedLambda:
                                                var lifetimeTagArguments = invocation.ArgumentList.Arguments.SkipWhile((arg, i) => arg.NameColon?.Name.Identifier.Text != "tags" && i < 1).ToList();
                                                metadataVisitor.VisitContract(
                                                    new MdContract(
                                                        semanticModel,
                                                        invocation,
                                                        null,
                                                        ContractKind.Explicit,
                                                        BuildTags(semanticModel, lifetimeTagArguments)));

                                                metadataVisitor.VisitLifetime(new MdLifetime(semanticModel, invocation, lifetime));

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

                                    if (typeArgumentsCount is 1 or 2 && symbol.TypeArguments is [.., {} resultType])
                                    {
                                        // .Transient(ctx => ..., tags)
                                        var lifetimeTagArguments = invocation.ArgumentList.Arguments.SkipWhile((arg, i) => arg.NameColon?.Name.Identifier.Text != "tags" && i < 1).ToList();
                                        metadataVisitor.VisitContract(
                                            new MdContract(
                                                semanticModel,
                                                invocation,
                                                null,
                                                ContractKind.Explicit,
                                                BuildTags(semanticModel, lifetimeTagArguments)));

                                        metadataVisitor.VisitLifetime(new MdLifetime(semanticModel, invocation, lifetime));

                                        VisitFactory(invocation, metadataVisitor, semanticModel, resultType, lambdaExpression);
                                        break;
                                    }
                                }

                                var tagArguments = invocation.ArgumentList.Arguments.SkipWhile((arg, i) => arg.NameColon?.Name.Identifier.Text != "tags" && i < 1).ToList();
                                metadataVisitor.VisitContract(
                                    new MdContract(
                                        semanticModel,
                                        invocation,
                                        null,
                                        ContractKind.Explicit,
                                        BuildTags(semanticModel, tagArguments)));

                                metadataVisitor.VisitLifetime(new MdLifetime(semanticModel, invocation, lifetime));

                                VisitFactory(invocation, metadataVisitor, semanticModel, type, lambdaExpression);
                                break;

                            case [{ Expression: {} expression }, ..]:
                                var expressionType = semantic.TryGetTypeSymbol<ITypeSymbol>(semanticModel, expression);
                                if (expressionType is INamedTypeSymbol { IsGenericType: true, TypeArguments.Length: 1 } expressionTypeSymbol
                                    && expressionTypeSymbol.TypeArguments is [{} expressionResultType])
                                {
                                    var lifetimeTagArguments = invocation.ArgumentList.Arguments.SkipWhile((arg, i) => arg.NameColon?.Name.Identifier.Text != "tags" && i < 1).ToList();
                                    metadataVisitor.VisitContract(
                                        new MdContract(
                                            semanticModel,
                                            invocation,
                                            null,
                                            ContractKind.Explicit,
                                            BuildTags(semanticModel, lifetimeTagArguments)));

                                    metadataVisitor.VisitLifetime(new MdLifetime(semanticModel, invocation, lifetime));

                                    // To(Guid.NewGuid, tags)
                                    VisitFactory(
                                        invocation,
                                        metadataVisitor,
                                        semanticModel,
                                        expressionResultType,
                                        SyntaxFactory.ParenthesizedLambdaExpression(
                                            SyntaxFactory.InvocationExpression(expression)));
                                }
                                else
                                {
                                    NotSupported(invocation);
                                }

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
                        var baseTypeDeclarationSyntax = invocation.Ancestors().OfType<BaseTypeDeclarationSyntax>().FirstOrDefault();
                        if (setupKind != CompositionKind.Global
                            && string.IsNullOrWhiteSpace(setupCompositionTypeName)
                            && baseTypeDeclarationSyntax is not null)
                        {
                            if (semanticModel.GetDeclaredSymbol(baseTypeDeclarationSyntax) is {} baseTypeSymbol)
                            {
                                setupCompositionTypeName = GetTypeName(baseTypeSymbol);
                            }
                            else
                            {
                                setupCompositionTypeName = baseTypeDeclarationSyntax.Identifier.Text;
                            }
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
                                ImmutableArray<MdSpecialType>.Empty,
                                ImmutableArray<MdAccumulator>.Empty,
                                ImmutableArray<SetupContextMembers>.Empty,
                                [],
                                comments.FilterHints(invocationComments).ToList(),
                                ImmutableArray<MdDefaultLifetime>.Empty));

                        if (baseTypeDeclarationSyntax is not null && semanticModel.GetDeclaredSymbol(baseTypeDeclarationSyntax) is {} baseType)
                        {
                            var derivedTypes = baseType.AllInterfaces.ToList();
                            var nestedBaseType = baseType.BaseType;
                            while (nestedBaseType != null)
                            {
                                derivedTypes.Add(nestedBaseType);
                                nestedBaseType = nestedBaseType.BaseType;
                            }

                            // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
                            foreach (var derivedType in derivedTypes)
                            {
                                var compositionName = new CompositionName(
                                    GetTypeName(derivedType),
                                    derivedType.ContainingNamespace.IsGlobalNamespace ? "" : derivedType.ContainingNamespace.ToString(),
                                    baseTypeDeclarationSyntax);

                                metadataVisitor.VisitDependsOn(
                                    new MdDependsOn(
                                        semanticModel,
                                        invocation,
                                        ImmutableArray.Create(new MdDependsOnItem(compositionName)),
                                        false));
                            }
                        }

                        break;

                    case nameof(IConfiguration.DefaultLifetime):
                        if (invocation.ArgumentList.Arguments is [{ Expression: {} defaultLifetimeSyntax }])
                        {
                            metadataVisitor.VisitDefaultLifetime(new MdDefaultLifetime(new MdLifetime(semanticModel, invocation, semantic.GetRequiredConstantValue<Lifetime>(semanticModel, defaultLifetimeSyntax))));
                        }

                        break;

                    case nameof(IConfiguration.DependsOn):
                        if (semanticModel.GetSymbolInfo(invocation).Symbol is IMethodSymbol { Parameters: [{ IsParams: false }, _, _ ] })
                        {
                            var dependsOnArgs = arguments.GetArgs(invocation.ArgumentList, "setupName", "kind", "name");
                            if (dependsOnArgs[0] is { Expression: {} setupNameExpression }
                                && dependsOnArgs[1] is { Expression: {} contextKindExpression })
                            {
                                var setupName = semantic.GetRequiredConstantValue<string>(semanticModel, setupNameExpression, SmartTagKind.Name);
                                var contextArgKind = semantic.GetRequiredConstantValue<SetupContextKind>(semanticModel, contextKindExpression);
                                var contextArgExpression = dependsOnArgs[2]?.Expression;
                                var contextArgName = contextArgExpression is not null
                                    ? semantic.GetRequiredConstantValue<string>(semanticModel, contextArgExpression, SmartTagKind.Name)
                                    : "";
                                if (string.IsNullOrWhiteSpace(contextArgName))
                                {
                                    if (contextArgKind == SetupContextKind.Members)
                                    {
                                        contextArgName = $"{Names.DefaultInstanceValueName}{Names.TempInstanceValueNameSuffix}";
                                    }
                                    else
                                    {
                                        throw new CompileErrorException(
                                            Strings.Error_SetupContextNameIsRequired,
                                            ImmutableArray.Create(locationProvider.GetLocation(invocation)),
                                            LogId.ErrorSetupContextNameIsRequired,
                                            nameof(Strings.Error_SetupContextNameIsRequired));
                                    }
                                }

                                metadataVisitor.VisitDependsOn(
                                    new MdDependsOn(
                                        semanticModel,
                                        invocation,
                                        ImmutableArray.Create(new MdDependsOnItem(
                                            CreateCompositionName(setupName, @namespace, invocation.ArgumentList),
                                            contextArgName,
                                            contextArgExpression,
                                            contextArgKind)),
                                        true));
                            }
                        }
                        else if (BuildConstantArgs<string>(semanticModel, invocation.ArgumentList.Arguments) is [..] compositionTypeNames)
                        {
                            metadataVisitor.VisitDependsOn(
                                new MdDependsOn(
                                    semanticModel,
                                    invocation,
                                    compositionTypeNames.Select(i => new MdDependsOnItem(CreateCompositionName(i, @namespace, invocation.ArgumentList))).ToImmutableArray(),
                                    true));
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
                                LogId.ErrorInvalidRootType,
                                nameof(Strings.Error_InvalidRootType));
                        }

                        var tagArguments = invocation.ArgumentList.Arguments.SkipWhile((arg, i) => arg.NameColon?.Name.Identifier.Text != "tags" && i < 2);
                        var tags = BuildTags(semanticModel, tagArguments);
                        VisitBind(metadataVisitor, semanticModel, invocation, tags, genericName);
                        var rootBindSymbol = semantic.GetTypeSymbol<INamedTypeSymbol>(semanticModel, rootBindType);
                        VisitRoot(invocation, tags.FirstOrDefault(), metadataVisitor, semanticModel, invocation, invocationComments, rootBindSymbol);
                        break;

                    case nameof(IBinding.To):
                        var toInvocationArgs = invocation.ArgumentList.Arguments;
                        var toInvocationTypeArgs = genericName.TypeArgumentList.Arguments;
                        switch (toInvocationTypeArgs)
                        {
                            // .To<T1, T2, ..., T>((dep1, dep2) => ..., tags)
                            case [.., not null, {} returnTypeSyntax] when toInvocationArgs is [{ Expression: LambdaExpressionSyntax lambdaExpression }]:
                                var returnType = semantic.GetTypeSymbol<ITypeSymbol>(semanticModel, returnTypeSyntax);
                                var args = toInvocationTypeArgs.ToList().GetRange(0, toInvocationTypeArgs.Count - 1);
                                VisitSimpleFactory(
                                    metadataVisitor,
                                    semanticModel,
                                    invocation,
                                    returnType,
                                    args,
                                    lambdaExpression);
                                break;

                            // To<T>(ctx => ...)
                            case [{} implementationTypeSyntax] when toInvocationArgs is [{ Expression: LambdaExpressionSyntax lambdaExpression }]:
                                var factoryType = semantic.GetTypeSymbol<ITypeSymbol>(semanticModel, implementationTypeSyntax);
                                VisitFactory(invocation, metadataVisitor, semanticModel, factoryType, lambdaExpression);
                                break;

                            // To<T>("code")
                            case [{} implementationTypeSyntax] when toInvocationArgs is [{ Expression: LiteralExpressionSyntax { Token.Value: string sourceCodeStatement } }]:
                                var lambda = SyntaxFactory
                                    .SimpleLambdaExpression(RootBuilder.DefaultCtxParameter)
                                    .WithExpressionBody(SyntaxFactory.IdentifierName(sourceCodeStatement));
                                factoryType = semantic.GetTypeSymbol<ITypeSymbol>(semanticModel, implementationTypeSyntax);
                                VisitFactory(invocation, metadataVisitor, semanticModel, factoryType, lambda);
                                break;

                            // .To<T>()
                            case [{} implementationTypeSyntax] when toInvocationArgs is []:
                                var implementationType = semantic.GetTypeSymbol<INamedTypeSymbol>(semanticModel, implementationTypeSyntax);
                                metadataVisitor.VisitImplementation(new MdImplementation(semanticModel, invocation, implementationType));
                                break;

                            default:
                                NotSupported(invocation);
                                break;
                        }

                        break;

                    case nameof(IConfiguration.Transient):
                    case nameof(IConfiguration.Singleton):
                    case nameof(IConfiguration.Scoped):
                    case nameof(IConfiguration.PerResolve):
                    case nameof(IConfiguration.PerBlock):
                        if (!Enum.TryParse<Lifetime>(genericName.Identifier.Text, out var bindingLifetime))
                        {
                            NotSupported(invocation);
                            break;
                        }

                        var lifetimeInvocationArgs = invocation.ArgumentList.Arguments;
                        var lifetimeInvocationTypeArgs = genericName.TypeArgumentList.Arguments;
                        switch (lifetimeInvocationTypeArgs)
                        {
                            // .To<T1, T2, ..., T>((dep1, dep2, ...) => ..., tags)
                            case [.., not null, {} returnTypeSyntax] when lifetimeInvocationArgs is [{ Expression: LambdaExpressionSyntax lambdaExpression }, ..]:
                                var lifetimeTagArguments = invocation.ArgumentList.Arguments.SkipWhile((arg, i) => arg.NameColon?.Name.Identifier.Text != "tags" && i < 1).ToList();
                                metadataVisitor.VisitContract(
                                    new MdContract(
                                        semanticModel,
                                        invocation,
                                        null,
                                        ContractKind.Explicit,
                                        BuildTags(semanticModel, lifetimeTagArguments)));

                                metadataVisitor.VisitLifetime(new MdLifetime(semanticModel, invocation, bindingLifetime));

                                var returnType = semantic.GetTypeSymbol<ITypeSymbol>(semanticModel, returnTypeSyntax);
                                var args = lifetimeInvocationTypeArgs.ToList().GetRange(0, lifetimeInvocationTypeArgs.Count - 1);
                                VisitSimpleFactory(
                                    metadataVisitor,
                                    semanticModel,
                                    invocation,
                                    returnType,
                                    args,
                                    lambdaExpression);
                                break;

                            // Transient<T>(ctx => ..., tags)
                            case [{} implementationTypeSyntax] when lifetimeInvocationArgs is [{ Expression: LambdaExpressionSyntax lambdaExpression }, ..]:
                                metadataVisitor.VisitContract(
                                    new MdContract(
                                        semanticModel,
                                        invocation,
                                        null,
                                        ContractKind.Explicit,
                                        BuildTags(semanticModel, invocation.ArgumentList.Arguments.SkipWhile((arg, i) => arg.NameColon?.Name.Identifier.Text != "tags" && i < 1))));

                                metadataVisitor.VisitLifetime(new MdLifetime(semanticModel, invocation, bindingLifetime));

                                var factoryType = semantic.GetTypeSymbol<ITypeSymbol>(semanticModel, implementationTypeSyntax);
                                VisitFactory(invocation, metadataVisitor, semanticModel, factoryType, lambdaExpression);
                                break;

                            default:
                                // .Transient<T>()
                                var lifetimesTags = BuildTags(semanticModel, lifetimeInvocationArgs);
                                foreach (var typeArgument in genericName.TypeArgumentList.Arguments)
                                {
                                    metadataVisitor.VisitContract(
                                        new MdContract(
                                            semanticModel,
                                            typeArgument,
                                            null,
                                            ContractKind.Explicit,
                                            lifetimesTags));

                                    metadataVisitor.VisitLifetime(new MdLifetime(semanticModel, typeArgument, bindingLifetime));

                                    var implementationType = semantic.GetTypeSymbol<INamedTypeSymbol>(semanticModel, typeArgument);
                                    metadataVisitor.VisitImplementation(new MdImplementation(semanticModel, typeArgument, implementationType));
                                }

                                break;
                        }

                        break;

                    case nameof(IConfiguration.Arg):
                        VisitArg(invocation, metadataVisitor, semanticModel, ArgKind.Composition, invocation, genericName, invocationComments);
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
                                LogId.ErrorInvalidRootsType,
                                nameof(Strings.Error_InvalidRootsRype));
                        }

                        var rootsArgs = arguments.GetArgs(invocation.ArgumentList, "name", "kind", "filter");
                        var rootsName = rootsArgs[0] is {} rootsNameArg ? semantic.GetConstantValue<object>(semanticModel, rootsNameArg.Expression, SmartTagKind.Name)?.ToString() ?? "" : "";
                        var rootsKind = rootsArgs[1] is {} rootsKindArg ? semantic.GetConstantValue<RootKinds>(semanticModel, rootsKindArg.Expression) : RootKinds.Default;
                        var rootsWildcardFilter = (rootsArgs[2] is {} rootsFilterArg ? semantic.GetConstantValue<string>(semanticModel, rootsFilterArg.Expression) : "*") ?? "*";
                        var hasRootsType = false;
                        foreach (var rootType in GetRelatedTypes(invocation, semanticModel, invocation, rootsType, rootsWildcardFilter))
                        {
                            var rootName = GetName((SyntaxNode?)rootsArgs[1] ?? invocation, rootsName, rootType) ?? "";
                            metadataVisitor.VisitRoot(new MdRoot(idGenerator.Generate(), invocation, semanticModel, rootType, rootName, nameProvider.GetUniqueRootName(rootName, rootType), new MdTag(0, null), rootsKind, invocationComments, rootsType, false));
                            hasRootsType = true;
                        }

                        if (!hasRootsType)
                        {
                            throw new CompileErrorException(
                                string.Format(Strings.Error_Template_NoTypeForWildcard, symbolNames.GetName(rootsType), rootsWildcardFilter),
                                ImmutableArray.Create(locationProvider.GetLocation(invocation)),
                                LogId.ErrorNoTypeForWildcard,
                                nameof(Strings.Error_Template_NoTypeForWildcard));
                        }

                        break;

                    case nameof(IConfiguration.Root):
                        if (genericName.TypeArgumentList.Arguments is not [{} rootTypeSyntax])
                        {
                            throw new CompileErrorException(
                                Strings.Error_InvalidRootType,
                                ImmutableArray.Create(locationProvider.GetLocation(invocation)),
                                LogId.ErrorInvalidRootType,
                                nameof(Strings.Error_InvalidRootType));
                        }

                        var rootSymbol = semantic.GetTypeSymbol<ITypeSymbol>(semanticModel, rootTypeSyntax);
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
                                LogId.ErrorInvalidBuildersType,
                                nameof(Strings.Error_InvalidBuildersType));
                        }

                        var buildersArgs = arguments.GetArgs(invocation.ArgumentList, "name", "kind", "filter");
                        var buildersName = buildersArgs[0] is {} buildersNameArg ? semantic.GetConstantValue<object>(semanticModel, buildersNameArg.Expression, SmartTagKind.Name)?.ToString() ?? Names.DefaultBuilderName : Names.DefaultBuilderName;
                        var buildersKind = buildersArgs[1] is {} buildersKindArg ? semantic.GetConstantValue<RootKinds>(semanticModel, buildersKindArg.Expression) : RootKinds.Default;
                        var buildersWildcardFilter = (buildersArgs[2] is {} buildersFilterArg ? semantic.GetConstantValue<string>(semanticModel, buildersFilterArg.Expression) : "*") ?? "*";
                        var builderRoots = (
                            from buildersType in GetRelatedTypes(invocation, semanticModel, invocation, buildersRootType, buildersWildcardFilter)
                            let buildersItemName = GetName((SyntaxNode?)buildersArgs[0] ?? invocation, buildersName, buildersType) ?? Names.DefaultBuilderName
                            select VisitBuilder(invocation, metadataVisitor, semanticModel, buildersType, buildersRootType, buildersItemName, buildersKind, invocationComments))
                            .ToList();

                        if (builderRoots.Count == 0)
                        {
                            throw new CompileErrorException(
                                string.Format(Strings.Error_Template_NoTypeForWildcard, symbolNames.GetName(buildersRootType), buildersWildcardFilter),
                                ImmutableArray.Create(locationProvider.GetLocation(invocation)),
                                LogId.ErrorNoTypeForWildcard,
                                nameof(Strings.Error_Template_NoTypeForWildcard));
                        }

                        // Composite builder
                        var builderArgId = idGenerator.Generate();
                        var builderArgTag = new MdTag(0, builderArgId + "BuilderArg" + Names.Salt);
                        var builderTag = new MdTag(0, builderArgId + "Builder" + Names.Salt);

                        // Building instance arg
                        metadataVisitor.VisitContract(new MdContract(semanticModel, invocation, buildersRootType, ContractKind.Explicit, ImmutableArray.Create(builderArgTag)));
                        metadataVisitor.VisitArg(new MdArg(semanticModel, invocation, buildersRootType, Names.BuildingInstance, ArgKind.Root, true, ["Instance for the build-up."]));

                        // Fake factory expression, it is actually implemented in RootsBuilder
                        var factory = new Lines();
                        factory.AppendLine($"({Names.IContextTypeName} {Names.ContextInstance}) =>");
                        using (factory.CreateBlock())
                        {
                            factory.AppendLine($"{Names.ContextInstance}.{nameof(IContext.Inject)}({builderArgTag.Value.ValueToString()}, out {symbolNames.GetName(buildersRootType)} {Names.BuildingInstance});");
                            factory.AppendLine($"return {Names.BuildingInstance};");
                        }

                        var builderLambdaExpression = (LambdaExpressionSyntax)SyntaxFactory.ParseExpression(factory.ToString());

                        metadataVisitor.VisitContract(new MdContract(semanticModel, invocation, buildersRootType, ContractKind.Explicit, ImmutableArray.Create(builderTag)));
                        VisitFactory(invocation, metadataVisitor, semanticModel, buildersRootType, builderLambdaExpression);

                        var builderRootName = GetName((SyntaxNode?)buildersArgs[0] ?? invocation, buildersName, buildersRootType) ?? Names.DefaultBuilderName;
                        var root = new MdRoot(idGenerator.Generate(), invocation, semanticModel, buildersRootType, builderRootName, nameProvider.GetUniqueRootName(builderRootName, buildersRootType), builderTag, buildersKind, invocationComments, buildersRootType, true, builderRoots.ToImmutableArray());
                        metadataVisitor.VisitRoot(root);
                        break;

                    case nameof(IConfiguration.Builder):
                        if (genericName.TypeArgumentList.Arguments is not [{} builderRootTypeSyntax])
                        {
                            throw new CompileErrorException(
                                Strings.Error_InvalidBuilderType,
                                ImmutableArray.Create(locationProvider.GetLocation(invocation)),
                                LogId.ErrorInvalidBuilderType,
                                nameof(Strings.Error_InvalidBuilderType));
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

                    case nameof(IConfiguration.SpecialType):
                        if (genericName.TypeArgumentList.Arguments is [{} specialTypeExpression]
                            && semantic.TryGetTypeSymbol<INamedTypeSymbol>(semanticModel, specialTypeExpression) is {} specialType)
                        {
                            metadataVisitor.VisitSpecialType(new MdSpecialType(semanticModel, specialTypeExpression, specialType));
                        }
                        else
                        {
                            NotSupported(invocation);
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
        string wildcardFilter) =>
        from type in semanticModel.LookupNamespacesAndTypes(invocation.Span.Start)
        where !type.IsAbstract
        let namedTypeSymbol = type as INamedTypeSymbol
        where namedTypeSymbol != null
        where !types.TypeEquals(baseType.ConstructedFrom, namedTypeSymbol.ConstructedFrom)
        let typeInfo = baseSymbolsProvider
            .GetBaseSymbols(namedTypeSymbol, (t, _) => t is INamedTypeSymbol)
            .FirstOrDefault(info => TypesMath(baseType, info))
        where typeInfo != null
        where wildcardMatcher.Match(wildcardFilter.AsSpan(), symbolNames.GetName(namedTypeSymbol).AsSpan())
        orderby typeInfo.Deepness descending
        select CreateBuilderType(source, baseType, namedTypeSymbol);

    private bool TypesMath(INamedTypeSymbol baseType, TypeInfo info)
    {
        return types.TypeEquals(baseType.ConstructedFrom, ((INamedTypeSymbol)info.Type).ConstructedFrom);
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
                    LogId.ErrorTooManyTypeParameters,
                    nameof(Strings.Error_TooManyTypeParameters));
            }
        }

        return typeSymbol.Construct(markers.ToArray());
    }

    private MdRoot VisitBuilder(
        InvocationExpressionSyntax source,
        IMetadataVisitor metadataVisitor,
        SemanticModel semanticModel,
        INamedTypeSymbol builderType,
        INamedTypeSymbol rootContractType,
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
        var factory = new Lines();
        factory.AppendLine($"({Names.IContextTypeName} {Names.ContextInstance}) =>");
        using (factory.CreateBlock())
        {
            factory.AppendLine($"{Names.ContextInstance}.{nameof(IContext.Inject)}({builderArgTag.Value.ValueToString()}, out {symbolNames.GetName(builderType)} {Names.BuildingInstance});");
            factory.AppendLine($"{Names.ContextInstance}.{nameof(IContext.BuildUp)}({Names.BuildingInstance});");
            factory.AppendLine($"return {Names.BuildingInstance};");
        }

        var builderLambdaExpression = (LambdaExpressionSyntax)SyntaxFactory.ParseExpression(factory.ToString());

        metadataVisitor.VisitContract(new MdContract(semanticModel, source, builderType, ContractKind.Explicit, ImmutableArray.Create(builderTag)));
        VisitFactory(source, metadataVisitor, semanticModel, builderType, builderLambdaExpression);

        // Root
        var root = new MdRoot(idGenerator.Generate(), source, semanticModel, builderType, builderName, nameProvider.GetUniqueRootName(builderName, builderType), builderTag, kind, invocationComments, rootContractType, true);
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
        ITypeSymbol rootSymbol)
    {
        var rootArgs = arguments.GetArgs(invocation.ArgumentList, "name", "tag", "kind");
        var tag = rootArgs[1] is {} tagArg ? semantic.GetConstantValue<object>(semanticModel, tagArg.Expression, SmartTagKind.Tag) : null;
        var name = rootArgs[0] is {} nameArg
            ? GetName(nameArg, semantic.GetConstantValue<object>(semanticModel, nameArg.Expression, SmartTagKind.Name)?.ToString(), rootSymbol, tag) ?? ""
            : "";
        var kind = rootArgs[2] is {} kindArg ? semantic.GetConstantValue<RootKinds>(semanticModel, kindArg.Expression) : RootKinds.Default;
        metadataVisitor.VisitRoot(new MdRoot(idGenerator.Generate(), source, semanticModel, rootSymbol, name, nameProvider.GetUniqueRootName(name, rootSymbol), new MdTag(0, tag), kind, invocationComments, rootSymbol, false));
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
        metadataVisitor.VisitRoot(new MdRoot(idGenerator.Generate(), source, semanticModel, rootSymbol, name, nameProvider.GetUniqueRootName(name, rootSymbol), tag, kind, invocationComments, rootSymbol, false));
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

            case ParenthesizedLambdaExpressionSyntax { ParameterList.Parameters: [] }:
                contextParameter = RootBuilder.DefaultCtxParameter;
                break;

            default:
                return;
        }

        var localVariableRenamingRewriter = localVariableRenamingRewriterFactory()!;
        var factoryApiWalker = factoryApiWalkerFactory();
        factoryApiWalker.Initialize(semanticModel, contextParameter, lambdaExpression);
        factoryApiWalker.Visit(lambdaExpression);
        var contextSymbol = contextParameter.SyntaxTree == semanticModel.SyntaxTree
            ? semanticModel.GetDeclaredSymbol(contextParameter)
            : null;

        var resolversHasContextTag = false;
        var resolvers = factoryApiWalker.Meta
            .Where(i => i.Kind == FactoryMetaKind.Resolver)
            .Select(meta => CreateResolver(semanticModel, resultType, meta, contextParameter, contextSymbol, ref resolversHasContextTag, localVariableRenamingRewriter))
            .Where(i => i != default)
            .ToImmutableArray();

        var initializersHasContextTag = false;
        var initializers = factoryApiWalker.Meta
            .Where(i => i.Kind == FactoryMetaKind.Initializer)
            .Select(meta => CreateInitializer(semanticModel, resultType, meta, contextParameter, contextSymbol, ref initializersHasContextTag, localVariableRenamingRewriter))
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
        ISymbol? contextSymbol,
        ILocalVariableRenamingRewriter localVariableRenamingRewriter,
        out bool hasContextTag)
    {
        hasContextTag = false;
        var invocation = @override.Expression;
        if (!IsContextInvocation(semanticModel, invocation, contextParameter, contextSymbol))
        {
            return default;
        }

        if (invocation.ArgumentList.Arguments.Count == 0)
        {
            return default;
        }

        var args = arguments.GetArgs(invocation.ArgumentList, "value", "tags");
        if (args[0] is not {} atgSyntax)
        {
            return default;
        }

        var isDeep = IsDeepOverride(invocation);

        var argType = GetDefaultType(semanticModel, invocation, 0) ?? GetArgSymbol(semanticModel, atgSyntax);
        if (argType is null or IErrorTypeSymbol)
        {
                    throw new CompileErrorException(
                        Strings.Error_TypeCannotBeInferred,
                        ImmutableArray.Create(locationProvider.GetLocation(atgSyntax)),
                        LogId.ErrorTypeCannotBeInferred,
                        nameof(Strings.Error_TypeCannotBeInferred));
        }

        var tagArguments = GetOverrideTagArguments(invocation).ToList();
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
                valueExpression = (ExpressionSyntax)localVariableRenamingRewriter.Rewrite(semanticModel, true, atgSyntax.Expression);
                break;
        }

        return new MdOverride(
            semanticModel,
            invocation,
            overrideIdProvider.GetId(argType, tags),
            @override.Position,
            argType,
            tags,
            valueExpression,
            isDeep,
            HasExplicitTypeArguments(invocation));
    }

    private static IEnumerable<ArgumentSyntax> GetOverrideTagArguments(InvocationExpressionSyntax invocation)
    {
        var args = invocation.ArgumentList.Arguments;
        // ReSharper disable once ConvertIfStatementToReturnStatement
        if (args.Any(arg => arg.NameColon?.Name.Identifier.Text == "tags"))
        {
            return args.Where(arg => arg.NameColon?.Name.Identifier.Text == "tags");
        }

        return args.Where((arg, index) => arg.NameColon is null && index > 0);
    }

    private static bool HasExplicitTypeArguments(InvocationExpressionSyntax invocation)
    {
        return invocation.Expression switch
        {
            MemberAccessExpressionSyntax { Name: GenericNameSyntax } => true,
            MemberBindingExpressionSyntax { Name: GenericNameSyntax } => true,
            GenericNameSyntax => true,
            _ => false
        };
    }

    private static bool IsDeepOverride(InvocationExpressionSyntax invocation)
    {
        var name = invocation.Expression switch
        {
            MemberAccessExpressionSyntax { Name: IdentifierNameSyntax identifier } => identifier.Identifier.Text,
            MemberAccessExpressionSyntax { Name: GenericNameSyntax generic } => generic.Identifier.Text,
            MemberBindingExpressionSyntax { Name: IdentifierNameSyntax identifier } => identifier.Identifier.Text,
            MemberBindingExpressionSyntax { Name: GenericNameSyntax generic } => generic.Identifier.Text,
            IdentifierNameSyntax identifier => identifier.Identifier.Text,
            GenericNameSyntax generic => generic.Identifier.Text,
            _ => null
        };

        return name != nameof(IContext.Let);
    }

    private MdInitializer CreateInitializer(
        SemanticModel semanticModel,
        ITypeSymbol resultType,
        FactoryMeta meta,
        ParameterSyntax contextParameter,
        ISymbol? contextSymbol,
        ref bool hasContextTag,
        ILocalVariableRenamingRewriter localVariableRenamingRewriter)
    {
        var invocation = meta.Expression;
        if (!IsContextInvocation(semanticModel, invocation, contextParameter, contextSymbol))
        {
            return default;
        }

        if (invocation.ArgumentList.Arguments is not [{} targetArg])
        {
            return default;
        }

        var targetType = GetArgSymbol(semanticModel, targetArg) ?? resultType;
        var overrides = new List<MdOverride>();
        // ReSharper disable once LoopCanBeConvertedToQuery
        foreach (var @override in meta.Overrides)
        {
            var mdOverride = CreateOverride(semanticModel, @override, contextParameter, contextSymbol, localVariableRenamingRewriter, out hasContextTag);
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
        ISymbol? contextSymbol,
        ref bool hasContextTag,
        ILocalVariableRenamingRewriter localVariableRenamingRewriter)
    {
        var invocation = meta.Expression;
        if (!IsContextInvocation(semanticModel, invocation, contextParameter, contextSymbol))
        {
            return default;
        }

        if (invocation.ArgumentList.Arguments is not { Count: > 0 } invArguments)
        {
            return default;
        }

        var overrides = new List<MdOverride>();
        // ReSharper disable once LoopCanBeConvertedToQuery
        foreach (var overrideInvocation in meta.Overrides)
        {
            var mdOverride = CreateOverride(semanticModel, overrideInvocation, contextParameter, contextSymbol, localVariableRenamingRewriter, out hasContextTag);
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
                            LogId.ErrorTypeCannotBeInferred,
                            nameof(Strings.Error_TypeCannotBeInferred));
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

    private static bool IsContextInvocation(
        SemanticModel semanticModel,
        InvocationExpressionSyntax invocation,
        ParameterSyntax contextParameter,
        ISymbol? contextSymbol)
    {
        if (invocation.Expression is not MemberAccessExpressionSyntax
            {
                Expression: IdentifierNameSyntax contextIdentifierName
            })
        {
            return false;
        }

        if (contextIdentifierName.Identifier.Text != contextParameter.Identifier.Text)
        {
            return false;
        }

        if (invocation.SyntaxTree != semanticModel.SyntaxTree)
        {
            return true;
        }

        if (contextParameter.SyntaxTree != semanticModel.SyntaxTree)
        {
            return false;
        }

        if (contextSymbol is null)
        {
            return false;
        }

        var symbol = semanticModel.GetSymbolInfo(contextIdentifierName).Symbol;
        return symbol is not null && SymbolEqualityComparer.Default.Equals(symbol, contextSymbol);
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

    private ITypeSymbol? GetArgSymbol(SemanticModel semanticModel, ArgumentSyntax argumentSyntax)
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

        var type = semantic.TryGetTypeSymbol<ITypeSymbol>(semanticModel, argumentSyntax.Expression);
        if (type is not null)
        {
            return type;
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
                LogId.ErrorAsyncFactoryNotSupported,
                nameof(Strings.Error_AsynchronousFactoryWithAsyncNotSupported));
        }
    }

    // ReSharper disable once SuggestBaseTypeForParameter
    private void NotSupported(SyntaxNode source) =>
        throw new CompileErrorException(
            string.Format(Strings.Error_Template_NotSupported, source),
            ImmutableArray.Create(locationProvider.GetLocation(source)),
            LogId.ErrorNotSupportedSyntax,
            nameof(Strings.Error_Template_NotSupported));

    private IReadOnlyList<T> BuildConstantArgs<T>(
        SemanticModel semanticModel,
        SeparatedSyntaxList<ArgumentSyntax> args)
    {
        var values = new List<T>(args.Count);
        foreach (var arg in args)
        {
            foreach (var value in semantic.GetConstantValues<T>(semanticModel, arg.Expression))
            {
                if (value is null)
                {
                    throw new CompileErrorException(
                        string.Format(Strings.Error_Template_MustBeValueOfType, arg.Expression, typeof(T)),
                        ImmutableArray.Create(locationProvider.GetLocation(arg.Expression)),
                        LogId.ErrorMustBeValueOfType,
                        nameof(Strings.Error_Template_MustBeValueOfType));
                }

                values.Add(value);
            }
        }

        return values;
    }

    private ImmutableArray<MdTag> BuildTags(
        SemanticModel semanticModel,
        IEnumerable<ArgumentSyntax> args)
    {
        var tags = new List<MdTag>();
        foreach (var arg in args)
        {
            foreach (var tag in semantic.GetConstantValues<object>(semanticModel, arg.Expression, SmartTagKind.Tag))
            {
                tags.Add(new MdTag(tags.Count, tag));
            }
        }

        return tags.ToImmutableArray();
    }

    private static CompositionName CreateCompositionName(
        string name,
        string ns,
        SyntaxNode source)
    {
        string className;
        string newNamespace;
        if (!string.IsNullOrWhiteSpace(name))
        {
            var trimmedName = name.Trim();
            var separatorIndex = trimmedName.LastIndexOf('.');
            if (separatorIndex >= 0)
            {
                className = separatorIndex + 1 < trimmedName.Length ? trimmedName[(separatorIndex + 1)..] : "";
                newNamespace = separatorIndex > 0 ? trimmedName[..separatorIndex] : "";
            }
            else
            {
                className = trimmedName;
                newNamespace = "";
            }
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

    private static string GetTypeName(INamedTypeSymbol typeSymbol)
    {
        var parts = new Stack<string>();
        var current = typeSymbol.OriginalDefinition;
        while (current != null)
        {
            var typeName = current.Name;
            if (current.TypeParameters.Length > 0)
            {
                typeName = $"{typeName}<{string.Join(", ", current.TypeParameters.Select(i => i.Name))}>";
            }

            parts.Push(typeName);
            current = current.ContainingType?.OriginalDefinition;
        }

        return string.Join(".", parts);
    }

    private string? GetName(
        SyntaxNode source,
        string? nameTemplate,
        ITypeSymbol? type = null,
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
                LogId.ErrorInvalidIdentifier,
                nameof(Strings.Error_Template_InvalidIdentifier));
        }

        return name;
    }
}
