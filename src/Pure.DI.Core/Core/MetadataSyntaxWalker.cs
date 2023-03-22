// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable ConvertToAutoPropertyWithPrivateSetter
// ReSharper disable TailRecursiveCall
// ReSharper disable HeapView.ObjectAllocation.Evident
// ReSharper disable HeapView.ObjectAllocation.Possible
// ReSharper disable HeapView.ObjectAllocation
// ReSharper disable InvertIf
// ReSharper disable HeapView.BoxingAllocation

namespace Pure.DI.Core;

using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

internal class MetadataSyntaxWalker : CSharpSyntaxWalker, IMetadataSyntaxWalker
{
    private static readonly Regex CommentRegex = new(@"//\s*(\w+)\s*=\s*(.+)\s*", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Singleline);
    private static readonly Settings EmptySettings = new();
    private static readonly ImmutableHashSet<string> ApiMethods = ImmutableHashSet.Create(
        nameof(DI.Setup),
        nameof(IConfiguration.Arg),
        nameof(IConfiguration.Bind),
        nameof(IConfiguration.DependsOn),
        nameof(IConfiguration.DefaultLifetime),
        nameof(IConfiguration.Root),
        nameof(IConfiguration.OrdinalAttribute),
        nameof(IConfiguration.TagAttribute),
        nameof(IConfiguration.TypeAttribute),
        nameof(IBinding.As),
        nameof(IBinding.To),
        nameof(IBinding.Tags)
    );
    
    private readonly ILogger<MetadataSyntaxWalker> _logger;
    private IMetadataVisitor? _metadataVisitor;
    private CancellationToken _cancellationToken = CancellationToken.None;
    private SemanticModel? _semanticModel;
    private readonly List<InvocationExpressionSyntax> _invocations = new();
    private readonly HashSet<string> _usingDirectives = new();
    private SyntaxNode? _next;
    private int _recursionDepth;
    private string _namespace = string.Empty;

    public MetadataSyntaxWalker(ILogger<MetadataSyntaxWalker> logger) => _logger = logger;

    private IMetadataVisitor MetadataVisitor => _metadataVisitor!;

    private SemanticModel SemanticModel => _semanticModel!;

    [SuppressMessage("MicrosoftCodeAnalysisCorrectness", "RS1024:Symbols should be compared for equality")]
    public void Visit(IMetadataVisitor metadataVisitor, in SyntaxUpdate update, in CancellationToken cancellationToken)
    {
        _metadataVisitor = metadataVisitor;
        _semanticModel = update.SemanticModel;
        _cancellationToken = cancellationToken;
        _invocations.Clear();
        _usingDirectives.Clear();
        _next = update.Node;
        do
        {
            _recursionDepth = 0;
            Visit(_next);
        } while (_next != default && _invocations.Any());

        foreach (var invocations in SplitInvocationsBySetups(_invocations))
        {
            foreach (var invocation in invocations)
            {
                ProcessInvocation(invocation);
            }
        }

        _invocations.Clear();
        metadataVisitor.VisitFinish();
    }

    private static IEnumerable<IEnumerable<InvocationExpressionSyntax>> SplitInvocationsBySetups(IEnumerable<InvocationExpressionSyntax> invocations)
    {
        var part = new List<InvocationExpressionSyntax>();
        foreach (var invocation in invocations)
        {
            part.Add(invocation);
            if (invocation.Expression is MemberAccessExpressionSyntax { Name: IdentifierNameSyntax { Identifier.Text: nameof(DI.Setup) } })
            {
                yield return part.AsEnumerable().Reverse();
                part.Clear();
            }
        }

        yield return part.AsEnumerable().Reverse();
    }

    // ReSharper disable once CognitiveComplexity
    public override void VisitInvocationExpression(InvocationExpressionSyntax invocation)
    {
        _next = default;
        if (_recursionDepth >= 20)
        {
            _next = invocation;
            return;
        }

        _cancellationToken.ThrowIfCancellationRequested();
        if (!IsMetadata(invocation))
        {
            return;
        }

        _recursionDepth++;
        _invocations.Add(invocation);
        base.VisitInvocationExpression(invocation);
    }

    private void ProcessInvocation(InvocationExpressionSyntax invocation)
    {
        _cancellationToken.ThrowIfCancellationRequested();
        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
        {
            return;
        }

        using var logToken = _logger.TraceProcess($"processing metadata \"{memberAccess.Name}\"", invocation.GetLocation());
        switch (memberAccess.Name)
        {
            case IdentifierNameSyntax identifierName:
                switch (identifierName.Identifier.Text)
                {
                    case nameof(IBinding.To):
                        switch (invocation.ArgumentList.Arguments)
                        {
                            case [{ Expression: SimpleLambdaExpressionSyntax lambdaExpression }]:
                                if (GetTypeSymbol<ITypeSymbol>(lambdaExpression) is INamedTypeSymbol
                                    {
                                        TypeArguments.Length: 2,
                                        TypeArguments: [_, { } resultType]
                                    })
                                {
                                    VisitFactory(resultType, lambdaExpression);
                                }
                                else
                                {
                                    NotSupported(invocation);
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
                            MetadataVisitor.VisitLifetime(new MdLifetime(SemanticModel, invocation, GetConstantValue<object>(lifetimeExpression)));
                        }

                        break;

                    case nameof(IBinding.Tags):
                        foreach (var tag in BuildTags(invocation.ArgumentList.Arguments))
                        {
                            MetadataVisitor.VisitTag(tag);
                        }

                        break;

                    case nameof(DI.Setup):
                        switch (invocation.ArgumentList.Arguments)
                        {
                            case [{ Expression: { } publicCompositionType }]:
                                MetadataVisitor.VisitSetup(
                                    new MdSetup(
                                        invocation,
                                        GetConstantValue<string>(publicCompositionType),
                                        _namespace,
                                        _usingDirectives.ToImmutableArray(),
                                        CompositionKind.Public,
                                        GetSettings(invocation),
                                        ImmutableArray<MdBinding>.Empty,
                                        ImmutableArray<MdRoot>.Empty,
                                        ImmutableArray<MdDependsOn>.Empty,
                                        ImmutableArray<MdTypeAttribute>.Empty,
                                        ImmutableArray<MdTagAttribute>.Empty,
                                        ImmutableArray<MdOrdinalAttribute>.Empty));

                                _namespace = string.Empty;
                                break;

                            case [{ Expression: { } publicCompositionType }, { Expression: { } kindExpression }]:
                                MetadataVisitor.VisitSetup(
                                    new MdSetup(
                                        invocation,
                                        GetConstantValue<string>(publicCompositionType),
                                        _namespace,
                                        _usingDirectives.ToImmutableArray(),
                                        GetConstantValue<CompositionKind>(kindExpression),
                                        GetSettings(invocation),
                                        ImmutableArray<MdBinding>.Empty,
                                        ImmutableArray<MdRoot>.Empty,
                                        ImmutableArray<MdDependsOn>.Empty,
                                        ImmutableArray<MdTypeAttribute>.Empty,
                                        ImmutableArray<MdTagAttribute>.Empty,
                                        ImmutableArray<MdOrdinalAttribute>.Empty));

                                _namespace = string.Empty;
                                break;

                            default:
                                NotSupported(invocation);
                                break;
                        }

                        break;

                    case nameof(IConfiguration.DefaultLifetime):
                        if (invocation.ArgumentList.Arguments is [{ Expression: { } defaultLifetimeSyntax }])
                        {
                            MetadataVisitor.VisitDefaultLifetime(new MdDefaultLifetime(new MdLifetime(SemanticModel, invocation, GetConstantValue<object>(defaultLifetimeSyntax))));
                        }

                        break;

                    case nameof(IConfiguration.DependsOn):
                        if (BuildConstantArgs<string>(invocation.ArgumentList.Arguments) is [..] compositionTypeNames)
                        {
                            MetadataVisitor.VisitDependsOn(new MdDependsOn(SemanticModel, invocation, compositionTypeNames.ToImmutableArray()));
                        }

                        break;
                }

                break;

            case GenericNameSyntax genericName:
                switch (genericName.Identifier.Text)
                {
                    case nameof(IConfiguration.Bind):
                        if (genericName.TypeArgumentList.Arguments is [{ } contractType])
                        {
                            MetadataVisitor.VisitContract(
                                new MdContract(
                                    SemanticModel,
                                    invocation,
                                    GetTypeSymbol<ITypeSymbol>(contractType),
                                    BuildTags(invocation.ArgumentList.Arguments).ToImmutable()));
                        }

                        break;

                    case nameof(IBinding.To):
                        if (genericName.TypeArgumentList.Arguments is [{ } implementationTypeName])
                        {
                            switch (invocation.ArgumentList.Arguments)
                            {
                                case [{ Expression: SimpleLambdaExpressionSyntax lambdaExpression }]:
                                    VisitFactory(GetTypeSymbol<ITypeSymbol>(implementationTypeName), lambdaExpression);
                                    break;

                                case []:
                                    MetadataVisitor.VisitImplementation(new MdImplementation(SemanticModel, implementationTypeName, GetTypeSymbol<INamedTypeSymbol>(implementationTypeName)));
                                    break;

                                default:
                                    NotSupported(invocation);
                                    break;
                            }
                        }

                        break;

                    case nameof(IConfiguration.Arg):
                        if (genericName.TypeArgumentList.Arguments is [{ } argTypeSyntax]
                            && invocation.ArgumentList.Arguments is [{ Expression: { } nameArgExpression }, ..] args)
                        {
                            var name = GetConstantValue<string>(nameArgExpression).Trim();
                            var tagsBuilder = ImmutableArray.CreateBuilder<MdTag>(args.Count - 1);
                            for (var index = 1; index < args.Count; index++)
                            {
                                var arg = args[index];
                                tagsBuilder.Add(new MdTag(SemanticModel, arg, index - 1, GetConstantValue<object>(arg.Expression)));
                            }

                            var argType = GetTypeSymbol<ITypeSymbol>(argTypeSyntax);
                            MetadataVisitor.VisitContract(new MdContract(SemanticModel, invocation, argType, tagsBuilder.SafeMoveToImmutable()));
                            MetadataVisitor.VisitArg(new MdArg(SemanticModel, invocation, argType, name));
                        }

                        break;

                    case nameof(IConfiguration.Root):
                        if (genericName.TypeArgumentList.Arguments is [{ } rootType]
                            && invocation.ArgumentList.Arguments is [{ Expression: { } nameExpression }, ..] rootArgs)
                        {
                            var rootSymbol = GetTypeSymbol<INamedTypeSymbol>(rootType);
                            var name = GetConstantValue<string>(nameExpression);
                            MdTag? tag = default;
                            if (rootArgs.Count == 2)
                            {
                                var tagArg = rootArgs[1];
                                tag = new MdTag(
                                    SemanticModel,
                                    tagArg,
                                    0,
                                    GetConstantValue<object>(tagArg.Expression));
                            }

                            MetadataVisitor.VisitRoot(new MdRoot(invocation, SemanticModel, rootSymbol, name, tag));
                        }

                        break;

                    case nameof(IConfiguration.TypeAttribute):
                        if (genericName.TypeArgumentList.Arguments is [{ } typeAttributeType])
                        {
                            MetadataVisitor.VisitTypeAttribute(new MdTypeAttribute(SemanticModel, invocation, GetTypeSymbol<ITypeSymbol>(typeAttributeType), BuildConstantArgs<object>(invocation.ArgumentList.Arguments) is [int positionVal] ? positionVal : 0));
                        }

                        break;

                    case nameof(IConfiguration.TagAttribute):
                        if (genericName.TypeArgumentList.Arguments is [{ } tagAttributeType])
                        {
                            MetadataVisitor.VisitTagAttribute(new MdTagAttribute(SemanticModel, invocation, GetTypeSymbol<ITypeSymbol>(tagAttributeType), BuildConstantArgs<object>(invocation.ArgumentList.Arguments) is [int positionVal] ? positionVal : 0));
                        }

                        break;

                    case nameof(IConfiguration.OrdinalAttribute):
                        if (genericName.TypeArgumentList.Arguments is [{ } ordinalAttributeType])
                        {
                            MetadataVisitor.VisitOrdinalAttribute(new MdOrdinalAttribute(SemanticModel, invocation, GetTypeSymbol<ITypeSymbol>(ordinalAttributeType), BuildConstantArgs<object>(invocation.ArgumentList.Arguments) is [int positionVal] ? positionVal : 0));
                        }

                        break;
                }

                break;
        }
    }

    public override void VisitUsingDirective(UsingDirectiveSyntax usingDirective)
    {
        _usingDirectives.Add(usingDirective.Name.ToFullString());
        base.VisitUsingDirective(usingDirective);
    }

    public override void VisitFileScopedNamespaceDeclaration(FileScopedNamespaceDeclarationSyntax namespaceDeclaration)
    {
        _namespace = namespaceDeclaration.Name.ToFullString();
        base.VisitFileScopedNamespaceDeclaration(namespaceDeclaration);
    }

    public override void VisitNamespaceDeclaration(NamespaceDeclarationSyntax namespaceDeclaration)
    {
        _namespace = namespaceDeclaration.Name.ToFullString();
        base.VisitNamespaceDeclaration(namespaceDeclaration);
    }

    private void VisitFactory(ITypeSymbol resultType, SimpleLambdaExpressionSyntax lambdaExpression)
    {
        var resolversWalker = new FactoryResolversSyntaxWalker();
        resolversWalker.Visit(lambdaExpression);
        var position = 0;
        var resolvers = resolversWalker.Select(invocation =>
        {
            if (invocation.Expression is MemberAccessExpressionSyntax
                {
                    Name: GenericNameSyntax
                    {
                        Identifier.Text: nameof(IContext.Inject),
                        TypeArgumentList.Arguments: [{ } resolverContractType]
                    }
                }
                && invocation.ArgumentList.Arguments is var arguments)
            {
                switch (arguments)
                {
                    case [{ RefOrOutKeyword.IsMissing: false } targetValue]:
                        return new MdResolver(
                            SemanticModel,
                            invocation,
                            position++,
                            GetTypeSymbol<ITypeSymbol>(resolverContractType),
                            default,
                            targetValue.Expression);

                    case [{ RefOrOutKeyword.IsMissing: false } tag, { RefOrOutKeyword.IsMissing: false } targetValue]:
                        var parentTag = 
                            tag.Expression is MemberAccessExpressionSyntax memberAccessExpression
                            && memberAccessExpression.IsKind(SyntaxKind.SimpleMemberAccessExpression)
                            && memberAccessExpression.Name.Identifier.Text == nameof(IContext.Tag)
                            && memberAccessExpression.Expression is IdentifierNameSyntax identifierName
                            && identifierName.Identifier.Text == lambdaExpression.Parameter.Identifier.Text;

                        return new MdResolver(
                            SemanticModel,
                            invocation,
                            position++,
                            GetTypeSymbol<ITypeSymbol>(resolverContractType),
                            new MdTag(SemanticModel, tag, 0, parentTag ? MdTag.ContextTag : GetConstantValue<object>(tag.Expression)),
                            targetValue.Expression);
                }
            }

            return default;
        });

        MetadataVisitor.VisitFactory(
            new MdFactory(
                SemanticModel,
                lambdaExpression,
                resultType,
                lambdaExpression,
                lambdaExpression.Parameter,
                resolvers.ToImmutableArray()));
    }

    private static ISettings GetSettings(SyntaxNode node)
    {
        if (!node.HasLeadingTrivia)
        {
            return EmptySettings;
        }

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

        if (!comments.Any())
        {
            return EmptySettings;
        }

        var settings = new Settings();
        foreach (var comment in comments)
        {
            if (!Enum.TryParse(comment.Groups[1].Value, true, out Setting setting))
            {
                continue;
            }

            settings[setting] = comment.Groups[2].Value;
        }

        return settings;
    }

    private T GetTypeSymbol<T>(SyntaxNode node)
        where T : ITypeSymbol
    {
        var typeInfo = SemanticModel.GetTypeInfo(node, _cancellationToken);
        var typeSymbol = typeInfo.Type ?? typeInfo.ConvertedType;
        if (typeSymbol is T symbol)
        {
            return symbol;
        }

        _logger.CompileError($"The type {node} is not supported.", node.GetLocation(), LogId.ErrorInvalidMetadata);
        throw HandledException.Shared;
    }

    // ReSharper disable once SuggestBaseTypeForParameter
    private void NotSupported(InvocationExpressionSyntax invocation)
    {
        _logger.CompileError($"The {invocation} is not supported.", invocation.GetLocation(), LogId.ErrorInvalidMetadata);
        throw HandledException.Shared;
    }

    private T GetConstantValue<T>(SyntaxNode node)
    {
        if (node is LiteralExpressionSyntax { Token.Value: T val })
        {
            return val;
        }

        if (
            node is MemberAccessExpressionSyntax memberAccessExpressionSyntax
            && memberAccessExpressionSyntax.IsKind(SyntaxKind.SimpleMemberAccessExpression))
        {
            var type = memberAccessExpressionSyntax.Expression.ToString();
            var enumValueStr = memberAccessExpressionSyntax.Name.Identifier.Text;
            if (type.EndsWith(nameof(CompositionKind)))
            {
                if (Enum.TryParse<CompositionKind>(enumValueStr, out var enumValue))
                {
                    return (T)(object)enumValue;
                }
            }
            else
            {
                if (type.EndsWith(nameof(Lifetime)))
                {
                    if (Enum.TryParse<Lifetime>(enumValueStr, out var enumValue))
                    {
                        return (T)(object)enumValue;
                    }
                }
            }
        }
        
        if (SemanticModel.GetConstantValue(node, _cancellationToken) is { HasValue: true, Value: { } value })
        {
            _logger.CompileInfo($"Consider using hardcoded value instead of {node} due to the performance impact.", node.GetLocation(), LogId.InfoPerformanceImpact);
            var type = SemanticModel.GetTypeInfo(node);
            if ((type.Type ?? type.ConvertedType) is { } typeSymbol)
            {
                if (Type.GetType(typeSymbol.ToString()) is { IsEnum: true } runtimeType)
                {
                    return (T)Enum.ToObject(runtimeType, value);
                }
            }

            if (value is T v)
            {
                return v;
            }
        }

        _logger.CompileError($"{node} must be a constant value of type {typeof(T)}.", node.GetLocation(), LogId.ErrorInvalidMetadata);
        throw HandledException.Shared;
    }

    private List<T> BuildConstantArgs<T>(SeparatedSyntaxList<ArgumentSyntax> arguments)
    {
        var values = new List<T>();
        // ReSharper disable once ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
        foreach (var argument in arguments)
        {
            values.Add(GetConstantValue<T>(argument.Expression));
        }

        return values;
    }

    private ImmutableArray<MdTag>.Builder BuildTags(SeparatedSyntaxList<ArgumentSyntax> arguments)
    {
        var builder = ImmutableArray.CreateBuilder<MdTag>();
        for (var index = 0; index < arguments.Count; index++)
        {
            var argument = arguments[index];
            builder.Add(new MdTag(SemanticModel, argument, index, GetConstantValue<object>(argument.Expression)));
        }

        return builder;
    }

    private static bool IsMetadata(InvocationExpressionSyntax invocation) =>
        invocation.Expression is MemberAccessExpressionSyntax memberAccess 
        && ApiMethods.Contains(memberAccess.Name.Identifier.Text);
}