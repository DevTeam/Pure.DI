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
                                BuildTags(semanticModel, invocation.ArgumentList.Arguments).ToImmutable()));
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
                        metadataVisitor.VisitSetup(
                            new MdSetup(
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
                        if (genericName.TypeArgumentList.Arguments is var contractTypes)
                        {
                            foreach (var contractType in contractTypes)
                            {
                                metadataVisitor.VisitContract(
                                    new MdContract(
                                        semanticModel,
                                        invocation.ArgumentList,
                                        semanticModel.GetTypeSymbol<ITypeSymbol>(contractType, cancellationToken),
                                        BuildTags(semanticModel, invocation.ArgumentList.Arguments).ToImmutable()));   
                            }
                        }

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
                        if (genericName.TypeArgumentList.Arguments is [{ } rootType])
                        {
                            var rootSymbol = semanticModel.GetTypeSymbol<INamedTypeSymbol>(rootType, cancellationToken);
                            var rootArgs = arguments.GetArgs(invocation.ArgumentList, "name", "tag", "kind");
                            var name = rootArgs[0] is {} nameArg ? semanticModel.GetConstantValue<string>(nameArg.Expression) ?? "" : "";
                            var tag = rootArgs[1] is {} tagArg ? semanticModel.GetConstantValue<object>(tagArg.Expression) : null;
                            var kind = rootArgs[2] is {} kindArg ? semanticModel.GetConstantValue<RootKinds>(kindArg.Expression) : RootKinds.Default;
                            metadataVisitor.VisitRoot(new MdRoot(rootType, semanticModel, rootSymbol, name, new MdTag(0, tag), kind, invocationComments));
                        }

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
                        if (genericName.TypeArgumentList.Arguments is [var typeSyntax, var accumulatorTypeSyntax] 
                            && arguments.GetArgs(invocation.ArgumentList, "lifetime") is [{ Expression: {} lifetimeArgExpression }])
                        {
                            var typeSymbol = semanticModel.GetTypeSymbol<INamedTypeSymbol>(typeSyntax, cancellationToken);
                            var accumulatorTypeSymbol = semanticModel.GetTypeSymbol<INamedTypeSymbol>(accumulatorTypeSyntax, cancellationToken);
                            metadataVisitor.VisitAccumulator(new MdAccumulator(semanticModel, invocation, typeSymbol, accumulatorTypeSymbol, semanticModel.GetConstantValue<Lifetime>(lifetimeArgExpression)));
                        }

                        break;
                }

                break;
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
            var tags = new List<MdTag>(args.Count - 1);
            for (var index = 1; index < args.Count; index++)
            {
                var arg = args[index];
                tags.Add(new MdTag(index - 1, semanticModel.GetConstantValue<object>(arg.Expression)));
            }

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

    private static List<T> BuildConstantArgs<T>(SemanticModel semanticModel, SeparatedSyntaxList<ArgumentSyntax> arguments)
    {
        var values = new List<T>();
        // ReSharper disable once ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
        foreach (var argument in arguments)
        {
            values.Add(semanticModel.GetRequiredConstantValue<T>(argument.Expression));
        }

        return values;
    }

    private static ImmutableArray<MdTag>.Builder BuildTags(SemanticModel semanticModel, SeparatedSyntaxList<ArgumentSyntax> arguments)
    {
        var builder = ImmutableArray.CreateBuilder<MdTag>();
        for (var index = 0; index < arguments.Count; index++)
        {
            var argument = arguments[index];
            var tag = semanticModel.GetConstantValue<object>(argument.Expression);
            builder.Add(new MdTag(index, tag));
        }

        return builder;
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