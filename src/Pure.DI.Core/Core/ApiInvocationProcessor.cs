// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable ConvertIfStatementToConditionalTernaryExpression
namespace Pure.DI.Core;

using Microsoft.CodeAnalysis.Operations;

internal class ApiInvocationProcessor(
    IComments comments,
    IArguments arguments,
    ISemantic semantic)
    : IApiInvocationProcessor
{
    private static readonly char[] TypeNamePartsSeparators = ['.'];

    public void ProcessInvocation(
        IMetadataVisitor metadataVisitor,
        SemanticModel semanticModel,
        InvocationExpressionSyntax invocation,
        string @namespace)
    {
        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
        {
            return;
        }

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
        
        switch (memberAccess.Name)
        {
            case IdentifierNameSyntax identifierName:
                switch (identifierName.Identifier.Text)
                {
                    case nameof(IConfiguration.Bind):
                        metadataVisitor.VisitContract(
                            new MdContract(
                                semanticModel,
                                invocation.ArgumentList,
                                default,
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
                                        && invocation.ArgumentList.Arguments[0].Expression is ParenthesizedLambdaExpressionSyntax { ParameterList.Parameters.Count: > 0 } parenthesizedLambdaExpressionSyntaxWithTypes
                                        && symbol.TypeArguments[0].ToDisplayString(NullableFlowState.None, SymbolDisplayFormat.FullyQualifiedFormat) != Names.ContextInterfaceName)
                                    {
                                        VisitSimpleFactory(
                                            metadataVisitor,
                                            semanticModel,
                                            invocation,
                                            symbol.TypeArguments.Last(),
                                            parenthesizedLambdaExpressionSyntaxWithTypes.ParameterList.Parameters.Select(i => i.Type!).ToList(),
                                            parenthesizedLambdaExpressionSyntaxWithTypes);
                                        
                                        break;
                                    }

                                    if (symbol.TypeArguments.Length == 2 && symbol.TypeArguments is [_, { } resultType])
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
                                NotSupported(invocation);
                                break;
                        }

                        break;

                    case nameof(IBinding.As):
                        if (invocation.ArgumentList.Arguments is [{ Expression: { } lifetimeExpression }])
                        {
                            metadataVisitor.VisitLifetime(new MdLifetime(semanticModel, invocation.ArgumentList, semantic.GetRequiredConstantValue<Lifetime>(semanticModel, lifetimeExpression)));
                        }

                        break;
                    
                    case nameof(IConfiguration.Hint):
                        var hintArgs = arguments.GetArgs(invocation.ArgumentList, "hint", "value");
                        if (hintArgs is [{ Expression: { } hintNameExpression }, { Expression: { } hintValueExpression }])
                        {
                            metadataVisitor.VisitHint(new MdHint(semantic.GetConstantValue<Hint>(semanticModel, hintNameExpression), semantic.GetRequiredConstantValue<string>(semanticModel, hintValueExpression)));
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
                                invocation.ArgumentList,
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
                        if (invocation.ArgumentList.Arguments is [{ Expression: { } defaultLifetimeSyntax }])
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
                        var tagArguments = invocation.ArgumentList.Arguments.SkipWhile((arg, i) => arg.NameColon?.Name.Identifier.Text != "tags" && i < 2);
                        var tags = BuildTags(semanticModel, tagArguments);
                        VisitBind(metadataVisitor, semanticModel, invocation, tags, genericName);
                        if (genericName.TypeArgumentList.Arguments is not [{ } rootBindType])
                        {
                            return;
                        }
                        
                        var rootBindSymbol = semantic.GetTypeSymbol<INamedTypeSymbol>(semanticModel, rootBindType);
                        VisitRoot(tags.FirstOrDefault(), metadataVisitor, semanticModel, invocation, invocationComments, rootBindSymbol);
                        break;

                    case nameof(IBinding.To):
                        if (genericName.TypeArgumentList.Arguments.Count > 1 
                            && invocation.ArgumentList.Arguments.Count == 1)
                        {
                            if (invocation.ArgumentList.Arguments[0].Expression is not ParenthesizedLambdaExpressionSyntax parenthesizedLambdaExpressionSyntax
                                || parenthesizedLambdaExpressionSyntax.ParameterList.Parameters.Count == 0)
                            {
                                NotSupported(invocation);
                                break;
                            }

                            VisitSimpleFactory(
                                metadataVisitor,
                                semanticModel,
                                invocation,
                                semantic.GetTypeSymbol<ITypeSymbol>(semanticModel, genericName.TypeArgumentList.Arguments.Last()),
                                genericName.TypeArgumentList.Arguments.Reverse().Skip(1).Reverse().ToList(),
                                parenthesizedLambdaExpressionSyntax);

                            break;
                        }

                        if (genericName.TypeArgumentList.Arguments is [{ } implementationTypeSyntax])
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
                                    NotSupported(invocation);
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

                    case nameof(IConfiguration.Root):
                        if (genericName.TypeArgumentList.Arguments is not [{ } rootType])
                        {
                            return;
                        }
        
                        var rootSymbol = semantic.GetTypeSymbol<INamedTypeSymbol>(semanticModel, rootType);
                        VisitRoot(metadataVisitor, semanticModel, invocation, invocationComments, rootSymbol);
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

    private void VisitSimpleFactory(
        IMetadataVisitor metadataVisitor,
        SemanticModel semanticModel,
        InvocationExpressionSyntax source,
        ITypeSymbol returnType,
        List<TypeSyntax> argsTypes,
        ParenthesizedLambdaExpressionSyntax lambdaExpression)
    {
        CheckNotAsync(lambdaExpression);
        var identifiers = lambdaExpression.ParameterList.Parameters.Select(i => i.Identifier).ToList();
        var paramAttributes = lambdaExpression.ParameterList.Parameters.Select(i => i.AttributeLists.SelectMany(j => j.Attributes).ToList()).ToList();
        const string ctxName = "ctx_1182D127";
        var contextParameter = SyntaxFactory.Parameter(SyntaxFactory.Identifier(ctxName));
        var resolvers = new List<MdResolver>();
        var block = new List<StatementSyntax>();
        for (var i = 0; i < argsTypes.Count; i++)
        {
            var argTypeSyntax = argsTypes[i];
            var argType = semantic.GetTypeSymbol<ITypeSymbol>(semanticModel, argTypeSyntax);
            var attributes = paramAttributes[i];
            resolvers.Add(new MdResolver
            {
                SemanticModel = semanticModel,
                Source = argTypeSyntax,
                ContractType = argType,
                Tag = new MdTag(0, null),
                Position = i,
                Attributes = attributes.ToImmutableArray()
            });

            var valueDeclaration = SyntaxFactory.DeclarationExpression(
                argTypeSyntax,
                SyntaxFactory.SingleVariableDesignation(identifiers[i]));

            var valueArg =
                SyntaxFactory.Argument(valueDeclaration)
                    .WithRefOrOutKeyword(SyntaxFactory.Token(SyntaxKind.OutKeyword));

            var injection = SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.IdentifierName(ctxName),
                        SyntaxFactory.IdentifierName(nameof(IContext.Inject))))
                .AddArgumentListArguments(valueArg);
                                
            block.Add(SyntaxFactory.ExpressionStatement(injection));
        }
        
        if (lambdaExpression.Block is {} lambdaBlock)
        {
            block.AddRange(lambdaBlock.Statements);
        }
        else
        {
            if (lambdaExpression.ExpressionBody is { } body)
            {
                block.Add(SyntaxFactory.ReturnStatement(body));
            }
        }
        
        var newLambdaExpression = SyntaxFactory.SimpleLambdaExpression(contextParameter)
            .WithBlock(SyntaxFactory.Block(block));

        metadataVisitor.VisitFactory(
            new MdFactory(
                semanticModel,
                source,
                returnType,
                newLambdaExpression,
                contextParameter,
                resolvers.ToImmutableArray(),
                false));
    }

    private bool TryGetAttributeType(
        GenericNameSyntax genericName,
        SemanticModel semanticModel,
        [NotNullWhen(true)] out INamedTypeSymbol? type)
    {
        if (genericName.TypeArgumentList.Arguments is not [{ } attributeTypeSyntax])
        {
            type = default;
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
        IMetadataVisitor metadataVisitor,
        SemanticModel semanticModel,
        InvocationExpressionSyntax invocation,
        IReadOnlyCollection<string> invocationComments,
        ITypeSymbol rootSymbol)
    {
        var rootArgs = arguments.GetArgs(invocation.ArgumentList, "name", "tag", "kind");
        var name = rootArgs[0] is { } nameArg ? semantic.GetConstantValue<string>(semanticModel, nameArg.Expression) ?? "" : "";
        var tag = rootArgs[1] is { } tagArg ? semantic.GetConstantValue<object>(semanticModel, tagArg.Expression) : null;
        var kind = rootArgs[2] is { } kindArg ? semantic.GetConstantValue<RootKinds>(semanticModel, kindArg.Expression) : RootKinds.Default;
        metadataVisitor.VisitRoot(new MdRoot(invocation, semanticModel, rootSymbol, name, new MdTag(0, tag), kind, invocationComments));
    }

    private void VisitRoot(
        MdTag? tag,
        IMetadataVisitor metadataVisitor,
        SemanticModel semanticModel,
        InvocationExpressionSyntax invocation,
        IReadOnlyCollection<string> invocationComments,
        ITypeSymbol rootSymbol)
    {
        tag ??= new MdTag(0, default);
        var rootArgs = arguments.GetArgs(invocation.ArgumentList, "name", "kind");
        var name = rootArgs[0] is { } nameArg ? semantic.GetConstantValue<string>(semanticModel, nameArg.Expression) ?? "" : "";
        var kind = rootArgs[1] is { } kindArg ? semantic.GetConstantValue<RootKinds>(semanticModel, kindArg.Expression) : RootKinds.Default;
        metadataVisitor.VisitRoot(new MdRoot(invocation, semanticModel, rootSymbol, name, tag, kind, invocationComments));
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
        if (genericName.TypeArgumentList.Arguments is [{ } argTypeSyntax]
            && invocation.ArgumentList.Arguments is [{ Expression: { } nameArgExpression }, ..] args)
        {
            var name = semantic.GetRequiredConstantValue<string>(semanticModel, nameArgExpression).Trim();
            var tags = BuildTags(semanticModel, args.Skip(1));
            var argType = semantic.GetTypeSymbol<ITypeSymbol>(semanticModel, argTypeSyntax);
            metadataVisitor.VisitContract(new MdContract(semanticModel, invocation, argType, ContractKind.Explicit, tags.ToImmutableArray()));
            metadataVisitor.VisitArg(new MdArg(semanticModel, argTypeSyntax, argType, name, kind, argComments));
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
        
        var resolversWalker = new FactoryResolversSyntaxWalker();
        resolversWalker.Visit(lambdaExpression);
        var position = 0;
        var hasContextTag = false;
        var resolvers = resolversWalker.Select(invocation =>
        {
            if (invocation.ArgumentList.Arguments is not { Count: > 0 } invArguments)
            {
                return default;
            }

            switch (invArguments)
            {
                case [{ RefOrOutKeyword.IsMissing: false } targetValue]:
                    if (semanticModel.GetOperation(invArguments[0]) is IArgumentOperation argumentOperation)
                    {
                        return new MdResolver(
                            semanticModel,
                            invocation,
                            position++,
                            argumentOperation.Value.Type!,
                            default,
                            targetValue.Expression);
                    }

                    break;

                default:
                    var args = arguments.GetArgs(invocation.ArgumentList, "tag", "value");
                    var tag = args[0]?.Expression;
                        
                    hasContextTag =
                        tag is MemberAccessExpressionSyntax memberAccessExpression
                        && memberAccessExpression.IsKind(SyntaxKind.SimpleMemberAccessExpression)
                        && memberAccessExpression.Name.Identifier.Text == nameof(IContext.Tag)
                        && memberAccessExpression.Expression is IdentifierNameSyntax identifierName
                        && identifierName.Identifier.Text == contextParameter.Identifier.Text;
                        
                    var resolverTag = new MdTag(
                        0, 
                        hasContextTag
                            ? MdTag.ContextTag
                            : tag is null 
                                ? default
                                : semantic.GetConstantValue<object>(semanticModel, tag));
                        
                    if (args[1] is {} valueArg
                        && semanticModel.GetOperation(valueArg) is IArgumentOperation { Value.Type: {} valueType })
                    {
                        return new MdResolver(
                            semanticModel,
                            invocation,
                            position++,
                            valueType,
                            resolverTag,
                            valueArg.Expression);
                    }

                    break;
            }

            return default;
        })
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
                lambdaExpression,
                contextParameter,
                resolvers,
                hasContextTag));
    }

    private static void CheckNotAsync(LambdaExpressionSyntax lambdaExpression)
    {
        if (lambdaExpression.AsyncKeyword.IsKind(SyntaxKind.AsyncKeyword))
        {
            throw new CompileErrorException("Asynchronous factory with the async keyword is not supported.", lambdaExpression.GetLocation(), LogId.ErrorInvalidMetadata);
        }
    }

    private static void VisitUsingDirectives(
        IMetadataVisitor metadataVisitor,
        SemanticModel semanticModel,
        SyntaxNode node)
    {
        var namespacesSyntaxWalker = new NamespacesSyntaxWalker(semanticModel);
        namespacesSyntaxWalker.Visit(node);
        var namespaces = namespacesSyntaxWalker.ToArray();
        if (namespaces.Any())
        {
            metadataVisitor.VisitUsingDirectives(new MdUsingDirectives(namespaces.ToImmutableArray(), ImmutableArray<string>.Empty));
        }
    }
    
    // ReSharper disable once SuggestBaseTypeForParameter
    private static void NotSupported(InvocationExpressionSyntax invocation) => 
        throw new CompileErrorException($"The {invocation} is not supported.", invocation.GetLocation(), LogId.ErrorInvalidMetadata);

    private IReadOnlyList<T> BuildConstantArgs<T>(
        SemanticModel semanticModel,
        SeparatedSyntaxList<ArgumentSyntax> args) =>
        args
            .SelectMany(a => semantic.GetConstantValues<T>(semanticModel, a.Expression).Select(value => (value, a.Expression)))
            .Select(a => a.value ?? throw new CompileErrorException(
                $"{a.Expression} must be a non-null value of type {typeof(T)}.", a.Expression.GetLocation(), LogId.ErrorInvalidMetadata))
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
}