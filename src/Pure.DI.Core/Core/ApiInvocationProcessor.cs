// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core;

using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.Operations;

internal class ApiInvocationProcessor(CancellationToken cancellationToken)
    : IApiInvocationProcessor
{
    private static readonly char[] TypeNamePartsSeparators = ['.'];
    private static readonly Regex CommentRegex = new(@"//\s*(\w+)\s*=\s*(.+)\s*", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Singleline);
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

        switch (memberAccess.Name)
        {
            case IdentifierNameSyntax identifierName:
                switch (identifierName.Identifier.Text)
                {
                    case nameof(IBinding.To):
                        switch (invocation.ArgumentList.Arguments)
                        {
                            case [{ Expression: SimpleLambdaExpressionSyntax lambdaExpression }]:
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
                        if (invocation.ArgumentList.Arguments is [{ Expression: { } hintNameExpression }, { Expression: { } hintValueExpression }])
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
                        switch (invocation.ArgumentList.Arguments)
                        {
                            case [{ Expression: { } publicCompositionType }]:
                                metadataVisitor.VisitSetup(
                                    new MdSetup(
                                        invocation.ArgumentList,
                                        CreateCompositionName(semanticModel.GetRequiredConstantValue<string>(publicCompositionType), @namespace, invocation.ArgumentList),
                                        ImmutableArray<MdUsingDirectives>.Empty,
                                        CompositionKind.Public,
                                        GetSettings(invocation),
                                        ImmutableArray<MdBinding>.Empty,
                                        ImmutableArray<MdRoot>.Empty,
                                        ImmutableArray<MdDependsOn>.Empty,
                                        ImmutableArray<MdTypeAttribute>.Empty,
                                        ImmutableArray<MdTagAttribute>.Empty,
                                        ImmutableArray<MdOrdinalAttribute>.Empty));
                                break;

                            case [{ Expression: { } publicCompositionType }, { Expression: { } kindExpression }]:
                                metadataVisitor.VisitSetup(
                                    new MdSetup(
                                        invocation.ArgumentList,
                                        CreateCompositionName(semanticModel.GetRequiredConstantValue<string>(publicCompositionType), @namespace, invocation.ArgumentList),
                                        ImmutableArray<MdUsingDirectives>.Empty,
                                        semanticModel.GetRequiredConstantValue<CompositionKind>(kindExpression),
                                        GetSettings(invocation),
                                        ImmutableArray<MdBinding>.Empty,
                                        ImmutableArray<MdRoot>.Empty,
                                        ImmutableArray<MdDependsOn>.Empty,
                                        ImmutableArray<MdTypeAttribute>.Empty,
                                        ImmutableArray<MdTagAttribute>.Empty,
                                        ImmutableArray<MdOrdinalAttribute>.Empty));
                                break;

                            default:
                                NotSupported(invocation);
                                break;
                        }

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
                        if (genericName.TypeArgumentList.Arguments is [{ } implementationTypeName])
                        {
                            switch (invocation.ArgumentList.Arguments)
                            {
                                case [{ Expression: SimpleLambdaExpressionSyntax lambdaExpression }]:
                                    VisitFactory(metadataVisitor, semanticModel, semanticModel.GetTypeSymbol<ITypeSymbol>(implementationTypeName, cancellationToken), lambdaExpression);
                                    break;
                                
                                case [{ Expression: LiteralExpressionSyntax { Token.Value: string sourceCodeStatement } }]:
                                    var lambda = SyntaxFactory
                                        .SimpleLambdaExpression(SyntaxFactory.Parameter(SyntaxFactory.Identifier("_")))
                                        .WithExpressionBody(SyntaxFactory.IdentifierName(sourceCodeStatement));
                                    VisitFactory(metadataVisitor, semanticModel, semanticModel.GetTypeSymbol<ITypeSymbol>(implementationTypeName, cancellationToken), lambda, true);
                                    break;

                                case []:
                                    metadataVisitor.VisitImplementation(new MdImplementation(semanticModel, implementationTypeName, semanticModel.GetTypeSymbol<INamedTypeSymbol>(implementationTypeName, cancellationToken)));
                                    break;

                                default:
                                    NotSupported(invocation);
                                    break;
                            }
                        }

                        break;

                    case nameof(IConfiguration.Arg):
                        VisitArg(metadataVisitor, semanticModel, ArgKind.Class, invocation, genericName);
                        break;
                    
                    case nameof(IConfiguration.RootArg):
                        VisitArg(metadataVisitor, semanticModel, ArgKind.Root, invocation, genericName);
                        break;

                    case nameof(IConfiguration.Root):
                        if (genericName.TypeArgumentList.Arguments is [{ } rootType])
                        {
                            var rootArgs = invocation.ArgumentList.Arguments;
                            var rootSymbol = semanticModel.GetTypeSymbol<INamedTypeSymbol>(rootType, cancellationToken);
                            var name = "";
                            if (rootArgs.Count >= 1)
                            {
                                name = semanticModel.GetRequiredConstantValue<string>(rootArgs[0].Expression);
                            }

                            MdTag? tag = default;
                            if (rootArgs.Count >= 2)
                            {
                                tag = new MdTag(0, semanticModel.GetConstantValue<object>(rootArgs[1].Expression));
                            }

                            var rootKind = RootKinds.Public;
                            if (rootArgs.Count >= 3)
                            {
                                rootKind = semanticModel.GetConstantValue<RootKinds>(rootArgs[2].Expression);
                            }

                            metadataVisitor.VisitRoot(new MdRoot(rootType, semanticModel, rootSymbol, name, tag, rootKind));
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
                }

                break;
        }
    }

    private void VisitArg(
        IMetadataVisitor metadataVisitor,
        SemanticModel semanticModel,
        ArgKind kind,
        InvocationExpressionSyntax invocation,
        GenericNameSyntax genericName)
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
            metadataVisitor.VisitArg(new MdArg(semanticModel, argTypeSyntax, argType, name, kind));
        }
    }

    private static void VisitFactory(
        IMetadataVisitor metadataVisitor,
        SemanticModel semanticModel,
        ITypeSymbol resultType,
        SimpleLambdaExpressionSyntax lambdaExpression,
        bool isManual = false)
    {
        var resolversWalker = new FactoryResolversSyntaxWalker();
        resolversWalker.Visit(lambdaExpression);
        var position = 0;
        var hasContextTag = false;
        var resolvers = resolversWalker.Select(invocation =>
        {
            // ReSharper disable once InvertIf
            if (invocation.ArgumentList.Arguments is { Count: > 0 } arguments)
            {
                switch (arguments)
                {
                    case [{ RefOrOutKeyword.IsMissing: false } targetValue]:
                        if (semanticModel.GetOperation(arguments[0]) is IArgumentOperation argumentOperation)
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

                    case [{ RefOrOutKeyword.IsMissing: false } tag, { RefOrOutKeyword.IsMissing: false } targetValue]:
                        hasContextTag = 
                            tag.Expression is MemberAccessExpressionSyntax memberAccessExpression
                            && memberAccessExpression.IsKind(SyntaxKind.SimpleMemberAccessExpression)
                            && memberAccessExpression.Name.Identifier.Text == nameof(IContext.Tag)
                            && memberAccessExpression.Expression is IdentifierNameSyntax identifierName 
                            && identifierName.Identifier.Text == lambdaExpression.Parameter.Identifier.Text;
                        
                        var resolverTag = new MdTag(0, hasContextTag ? MdTag.ContextTag : semanticModel.GetConstantValue<object>(tag.Expression));
                        if (arguments.Count > 0 && semanticModel.GetOperation(arguments[1]) is IArgumentOperation argumentOperation2)
                        {
                            return new MdResolver(
                                semanticModel,
                                invocation,
                                position++,
                                argumentOperation2.Value.Type!,
                                resolverTag,
                                targetValue.Expression);
                        }

                        break;
                }
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
                lambdaExpression.Parameter,
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

    private IHints GetSettings(SyntaxNode node)
    {
        var comments = (
                from trivia in node.GetLeadingTrivia()
                where trivia.IsKind(SyntaxKind.SingleLineCommentTrivia)
                select trivia.ToFullString().Trim()
                into comment
                select CommentRegex.Match(comment)
                into match
                where match.Success
                select match)
            .ToArray();

        foreach (var comment in comments)
        {
            if (!Enum.TryParse(comment.Groups[1].Value, true, out Hint setting))
            {
                continue;
            }

            _hints[setting] = comment.Groups[2].Value;
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
            builder.Add(new MdTag(index, semanticModel.GetConstantValue<object>(argument.Expression)));
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