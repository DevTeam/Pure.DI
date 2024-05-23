// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core;

using Microsoft.CodeAnalysis.Operations;

internal class ApiInvocationProcessor(
    IComments comments,
    IArguments arguments,
    CancellationToken cancellationToken)
    : IApiInvocationProcessor
{
    private static readonly char[] TypeNamePartsSeparators = ['.'];
    private readonly Hints _hints = new();

    public void ProcessInvocation(
        IMetadataVisitor metadataVisitor,
        SemanticModel semanticModel,
        InvocationExpressionSyntax invocation,
        string @namespace)
    {
        cancellationToken.ThrowIfCancellationRequested();
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
                                BuildTags(semanticModel, invocation.ArgumentList.Arguments)));
                        break;
                    
                    case nameof(IBinding.To):
                        switch (invocation.ArgumentList.Arguments)
                        {
                            case [{ Expression: LambdaExpressionSyntax lambdaExpression }]:
                                var type = semanticModel.TryGetTypeSymbol<ITypeSymbol>(lambdaExpression, cancellationToken) ?? semanticModel.GetTypeSymbol<ITypeSymbol>(lambdaExpression.Body, cancellationToken);
                                // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
                                if (type is INamedTypeSymbol { TypeArguments.Length: 2, TypeArguments: [_, { } resultType] })
                                {
                                    VisitFactory(metadataVisitor, semanticModel, resultType, lambdaExpression);
                                }
                                else
                                {
                                    VisitFactory(metadataVisitor, semanticModel, type, lambdaExpression);
                                }
                                
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
                            metadataVisitor.VisitLifetime(new MdLifetime(semanticModel, invocation.ArgumentList, semanticModel.GetRequiredConstantValue<Lifetime>(lifetimeExpression)));
                        }

                        break;
                    
                    case nameof(IConfiguration.Hint):
                        var hintArgs = arguments.GetArgs(invocation.ArgumentList, "hint", "value");
                        if (hintArgs is [{ Expression: { } hintNameExpression }, { Expression: { } hintValueExpression }])
                        {
                            _hints[semanticModel.GetConstantValue<Hint>(hintNameExpression)] = semanticModel.GetRequiredConstantValue<string>(hintValueExpression);
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
                        var setupCompositionTypeName = setupArgs[0] is {} compositionTypeNameArg ? semanticModel.GetRequiredConstantValue<string>(compositionTypeNameArg.Expression) : "";
                        var setupKind = setupArgs[1] is {} setupKindArg ? semanticModel.GetRequiredConstantValue<CompositionKind>(setupKindArg.Expression) : CompositionKind.Public;
                        if (setupKind != CompositionKind.Global
                            && string.IsNullOrWhiteSpace(setupCompositionTypeName)
                            && invocation.Ancestors().OfType<BaseTypeDeclarationSyntax>().FirstOrDefault() is {} baseType)
                        {
                            setupCompositionTypeName = baseType.Identifier.Text;
                        }
                        
                        metadataVisitor.VisitSetup(
                            new MdSetup(
                                semanticModel,
                                invocation.ArgumentList,
                                CreateCompositionName(setupCompositionTypeName, @namespace, invocation.ArgumentList),
                                ImmutableArray<MdUsingDirectives>.Empty,
                                setupKind,
                                ApplyHints(invocationComments),
                                ImmutableArray<MdBinding>.Empty,
                                ImmutableArray<MdRoot>.Empty,
                                ImmutableArray<MdDependsOn>.Empty,
                                ImmutableArray<MdTypeAttribute>.Empty,
                                ImmutableArray<MdTagAttribute>.Empty,
                                ImmutableArray<MdOrdinalAttribute>.Empty,
                                ImmutableArray<MdAccumulator>.Empty,
                                comments.FilterHints(invocationComments).ToList()));
                        break;
                        
                    case nameof(IConfiguration.DefaultLifetime):
                        if (invocation.ArgumentList.Arguments is [{ Expression: { } defaultLifetimeSyntax }])
                        {
                            metadataVisitor.VisitDefaultLifetime(new MdDefaultLifetime(new MdLifetime(semanticModel, invocation.ArgumentList, semanticModel.GetRequiredConstantValue<Lifetime>(defaultLifetimeSyntax))));
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
                        VisitBind(metadataVisitor, semanticModel, invocation, genericName, cancellationToken);
                        break;

                    case nameof(IConfiguration.RootBind):
                        var tagArguments = invocation.ArgumentList.Arguments.SkipWhile((arg, i) => arg.NameColon?.Name.Identifier.Text != "tags" && i < 2);
                        var tags = BuildTags(semanticModel, tagArguments);
                        VisitBind(metadataVisitor, semanticModel, invocation, tags, genericName, cancellationToken);
                        if (genericName.TypeArgumentList.Arguments is not [{ } rootBindType])
                        {
                            return;
                        }
                        
                        var rootBindSymbol = semanticModel.GetTypeSymbol<INamedTypeSymbol>(rootBindType, cancellationToken);
                        VisitRoot(arguments, tags.FirstOrDefault(), metadataVisitor, semanticModel, invocation, invocationComments, rootBindSymbol);
                        break;

                    case nameof(IBinding.To):
                        if (genericName.TypeArgumentList.Arguments is [{ } implementationTypeSyntax])
                        {
                            switch (invocation.ArgumentList.Arguments)
                            {
                                case [{ Expression: LambdaExpressionSyntax lambdaExpression }]:
                                    VisitFactory(metadataVisitor, semanticModel, semanticModel.GetTypeSymbol<ITypeSymbol>(implementationTypeSyntax, cancellationToken), lambdaExpression);
                                    break;
                                
                                case [{ Expression: LiteralExpressionSyntax { Token.Value: string sourceCodeStatement } }]:
                                    var lambda = SyntaxFactory
                                        .SimpleLambdaExpression(SyntaxFactory.Parameter(SyntaxFactory.Identifier("_")))
                                        .WithExpressionBody(SyntaxFactory.IdentifierName(sourceCodeStatement));
                                    VisitFactory(metadataVisitor, semanticModel, semanticModel.GetTypeSymbol<ITypeSymbol>(implementationTypeSyntax, cancellationToken), lambda, true);
                                    break;

                                case []:
                                    metadataVisitor.VisitImplementation(new MdImplementation(semanticModel, implementationTypeSyntax, semanticModel.GetTypeSymbol<INamedTypeSymbol>(implementationTypeSyntax, cancellationToken)));
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
        
                        var rootSymbol = semanticModel.GetTypeSymbol<INamedTypeSymbol>(rootType, cancellationToken);
                        VisitRoot(arguments, metadataVisitor, semanticModel, invocation, invocationComments, rootSymbol);
                        break;

                    case nameof(IConfiguration.TypeAttribute):
                        if (genericName.TypeArgumentList.Arguments is [{ } typeAttributeType])
                        {
                            metadataVisitor.VisitTypeAttribute(new MdTypeAttribute(semanticModel, invocation.ArgumentList, semanticModel.GetTypeSymbol<ITypeSymbol>(typeAttributeType, cancellationToken), BuildConstantArgs<object>(semanticModel, invocation.ArgumentList.Arguments) is [int positionVal] ? positionVal : 0));
                        }

                        break;

                    case nameof(IConfiguration.TagAttribute):
                        if (genericName.TypeArgumentList.Arguments is [{ } tagAttributeType])
                        {
                            metadataVisitor.VisitTagAttribute(new MdTagAttribute(semanticModel, invocation.ArgumentList, semanticModel.GetTypeSymbol<ITypeSymbol>(tagAttributeType, cancellationToken), BuildConstantArgs<object>(semanticModel, invocation.ArgumentList.Arguments) is [int positionVal] ? positionVal : 0));
                        }

                        break;

                    case nameof(IConfiguration.OrdinalAttribute):
                        if (genericName.TypeArgumentList.Arguments is [{ } ordinalAttributeType])
                        {
                            metadataVisitor.VisitOrdinalAttribute(new MdOrdinalAttribute(semanticModel, invocation.ArgumentList, semanticModel.GetTypeSymbol<ITypeSymbol>(ordinalAttributeType, cancellationToken), BuildConstantArgs<object>(semanticModel, invocation.ArgumentList.Arguments) is [int positionVal] ? positionVal : 0));
                        }

                        break;
                    
                    case nameof(IConfiguration.Accumulate):
                        if (genericName.TypeArgumentList.Arguments is [var typeSyntax, var accumulatorTypeSyntax])
                        {
                            var lifetimes = invocation.ArgumentList.Arguments
                                .SelectMany(i => semanticModel.GetConstantValues<Lifetime>(i.Expression))
                                .Distinct()
                                .OrderBy(i => i)
                                .ToList();

                            if (lifetimes.Count == 0)
                            {
                                lifetimes.AddRange(Enum.GetValues(typeof(Lifetime)).Cast<Lifetime>());
                            }
                            
                            var typeSymbol = semanticModel.GetTypeSymbol<INamedTypeSymbol>(typeSyntax, cancellationToken);
                            var accumulatorTypeSymbol = semanticModel.GetTypeSymbol<INamedTypeSymbol>(accumulatorTypeSyntax, cancellationToken);
                            foreach (var lifetime in lifetimes)
                            {
                                metadataVisitor.VisitAccumulator(new MdAccumulator(semanticModel, invocation, typeSymbol, accumulatorTypeSymbol, lifetime));   
                            }
                        }

                        break;
                }

                break;
        }
    }

    private static void VisitRoot(
        IArguments arguments,
        IMetadataVisitor metadataVisitor,
        SemanticModel semanticModel,
        InvocationExpressionSyntax invocation,
        IReadOnlyCollection<string> invocationComments,
        ITypeSymbol rootSymbol)
    {
        var rootArgs = arguments.GetArgs(invocation.ArgumentList, "name", "tag", "kind");
        var name = rootArgs[0] is { } nameArg ? semanticModel.GetConstantValue<string>(nameArg.Expression) ?? "" : "";
        var tag = rootArgs[1] is { } tagArg ? semanticModel.GetConstantValue<object>(tagArg.Expression) : null;
        var kind = rootArgs[2] is { } kindArg ? semanticModel.GetConstantValue<RootKinds>(kindArg.Expression) : RootKinds.Default;
        metadataVisitor.VisitRoot(new MdRoot(invocation, semanticModel, rootSymbol, name, new MdTag(0, tag), kind, invocationComments));
    }

    private static void VisitRoot(
        IArguments arguments,
        MdTag? tag,
        IMetadataVisitor metadataVisitor,
        SemanticModel semanticModel,
        InvocationExpressionSyntax invocation,
        IReadOnlyCollection<string> invocationComments,
        ITypeSymbol rootSymbol)
    {
        tag ??= new MdTag(0, default);
        var rootArgs = arguments.GetArgs(invocation.ArgumentList, "name", "kind");
        var name = rootArgs[0] is { } nameArg ? semanticModel.GetConstantValue<string>(nameArg.Expression) ?? "" : "";
        var kind = rootArgs[1] is { } kindArg ? semanticModel.GetConstantValue<RootKinds>(kindArg.Expression) : RootKinds.Default;
        metadataVisitor.VisitRoot(new MdRoot(invocation, semanticModel, rootSymbol, name, tag, kind, invocationComments));
    }

    private static void VisitBind(
        IMetadataVisitor metadataVisitor,
        SemanticModel semanticModel,
        InvocationExpressionSyntax invocation,
        GenericNameSyntax genericName,
        CancellationToken cancellationToken)
    {
        var tags = BuildTags(semanticModel, invocation.ArgumentList.Arguments);
        VisitBind(metadataVisitor, semanticModel, invocation, tags, genericName, cancellationToken);
    }

    private static void VisitBind(
        IMetadataVisitor metadataVisitor,
        SemanticModel semanticModel,
        InvocationExpressionSyntax invocation,
        ImmutableArray<MdTag> tags,
        GenericNameSyntax genericName,
        CancellationToken cancellationToken)
    {
        var contractTypes = genericName.TypeArgumentList.Arguments;
        foreach (var contractType in contractTypes)
        {
            metadataVisitor.VisitContract(
                new MdContract(
                    semanticModel,
                    invocation.ArgumentList,
                    semanticModel.GetTypeSymbol<ITypeSymbol>(contractType, cancellationToken),
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
            var name = semanticModel.GetRequiredConstantValue<string>(nameArgExpression).Trim();
            var tags = BuildTags(semanticModel, args.Skip(1));
            var argType = semanticModel.GetTypeSymbol<ITypeSymbol>(argTypeSyntax, cancellationToken);
            metadataVisitor.VisitContract(new MdContract(semanticModel, invocation, argType, tags.ToImmutableArray()));
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
                                : semanticModel.GetConstantValue<object>(tag));
                        
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
    
    private IHints ApplyHints(IEnumerable<string> invocationComments)
    {
        foreach (var hint in comments.GetHints(invocationComments))
        {
            _hints[hint.Key] = hint.Value;
        }

        return _hints;
    }
    
    // ReSharper disable once SuggestBaseTypeForParameter
    private static void NotSupported(InvocationExpressionSyntax invocation)
    {
        throw new CompileErrorException($"The {invocation} is not supported.", invocation.GetLocation(), LogId.ErrorInvalidMetadata);
    }

    private static IReadOnlyList<T> BuildConstantArgs<T>(SemanticModel semanticModel, SeparatedSyntaxList<ArgumentSyntax> arguments)
    {
        return arguments
            .SelectMany(a => semanticModel.GetConstantValues<T>(a.Expression).Select(value => (value, a.Expression)))
            .Select(a => a.value ?? throw new CompileErrorException(
                $"{a.Expression} must be a non-null value of type {typeof(T)}.", a.Expression.GetLocation(), LogId.ErrorInvalidMetadata))
            .ToList();
    }

    private static ImmutableArray<MdTag> BuildTags(SemanticModel semanticModel, IEnumerable<ArgumentSyntax> arguments)
    {
        return arguments
            .SelectMany(t => semanticModel.GetConstantValues<object>(t.Expression))
            .Select((tag, i) => new MdTag(i, tag))
            .ToImmutableArray();
    }

    private static CompositionName CreateCompositionName(string name, string ns, SyntaxNode source)
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