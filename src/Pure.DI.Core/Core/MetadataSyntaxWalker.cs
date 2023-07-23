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
using Microsoft.CodeAnalysis.Operations;

internal class MetadataSyntaxWalker : CSharpSyntaxWalker, IMetadataSyntaxWalker
{
    private const string DISetup = $"{nameof(DI)}.{nameof(DI.Setup)}";
    private static readonly char[] TypeNamePartsSeparators = { '.' };
    private static readonly Regex CommentRegex = new(@"//\s*(\w+)\s*=\s*(.+)\s*", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Singleline);
    private static readonly ImmutableHashSet<string> ApiMethods = ImmutableHashSet.Create(
        DISetup,
        nameof(IConfiguration.Arg),
        nameof(IConfiguration.Bind),
        nameof(IConfiguration.DependsOn),
        nameof(IConfiguration.DefaultLifetime),
        nameof(IConfiguration.Root),
        nameof(IConfiguration.Hint),
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
    private readonly List<UsingDirectiveSyntax> _usingDirectives = new();
    private readonly HashSet<string> _namespaces = new();
    private readonly Stack<InvocationExpressionSyntax> _invocations = new();
    private string _namespace = string.Empty;
    private readonly Hints _hints = new();

    public MetadataSyntaxWalker(ILogger<MetadataSyntaxWalker> logger) => _logger = logger;

    private IMetadataVisitor MetadataVisitor => _metadataVisitor!;

    private SemanticModel SemanticModel => _semanticModel!;

    [SuppressMessage("MicrosoftCodeAnalysisCorrectness", "RS1024:Symbols should be compared for equality")]
    public void Visit(IMetadataVisitor metadataVisitor, in SyntaxUpdate update, in CancellationToken cancellationToken)
    {
        _metadataVisitor = metadataVisitor;
        _semanticModel = update.SemanticModel;
        _cancellationToken = cancellationToken;
        _usingDirectives.Clear();
        Visit(update.Node);
        var invocations = new Stack<InvocationExpressionSyntax>();
        while (_invocations.TryPop(out var invocation))
        {
            invocations.Push(invocation);
            base.VisitInvocationExpression(invocation);
        }

        while (invocations.TryPop(out var invocation))
        {
            ProcessInvocation(invocation);
        }

        metadataVisitor.VisitFinish();
    }
    
    // ReSharper disable once CognitiveComplexity
    public override void VisitInvocationExpression(InvocationExpressionSyntax invocation)
    {
        _cancellationToken.ThrowIfCancellationRequested();
        if (_invocations.Count > 0 || IsMetadata(invocation))
        {
            _invocations.Push(invocation);
        }
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
                                var type = TryGetTypeSymbol<ITypeSymbol>(lambdaExpression) ?? GetTypeSymbol<ITypeSymbol>(lambdaExpression.Body);
                                // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
                                if (type is INamedTypeSymbol { TypeArguments.Length: 2, TypeArguments: [_, { } resultType] })
                                {
                                    VisitFactory(resultType, lambdaExpression);
                                }
                                else
                                {
                                    VisitFactory(type, lambdaExpression);
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
                            MetadataVisitor.VisitLifetime(new MdLifetime(SemanticModel, invocation, GetRequiredConstantValue<Lifetime>(lifetimeExpression)));
                        }

                        break;
                    
                    case nameof(IConfiguration.Hint):
                        if (invocation.ArgumentList.Arguments is [{ Expression: { } hintNameExpression }, { Expression: { } hintValueExpression }])
                        {
                            _hints[GetConstantValue<Hint>(hintNameExpression)] = GetRequiredConstantValue<string>(hintValueExpression);
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
                                        CreateCompositionName(GetRequiredConstantValue<string>(publicCompositionType), _namespace),
                                        GetUsingDirectives(invocation),
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
                                MetadataVisitor.VisitSetup(
                                    new MdSetup(
                                        invocation,
                                        CreateCompositionName(GetRequiredConstantValue<string>(publicCompositionType), _namespace),
                                        GetUsingDirectives(invocation),
                                        GetRequiredConstantValue<CompositionKind>(kindExpression),
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
                            MetadataVisitor.VisitDefaultLifetime(new MdDefaultLifetime(new MdLifetime(SemanticModel, invocation, GetRequiredConstantValue<Lifetime>(defaultLifetimeSyntax))));
                        }

                        break;

                    case nameof(IConfiguration.DependsOn):
                        if (BuildConstantArgs<string>(invocation.ArgumentList.Arguments) is [..] compositionTypeNames)
                        {
                            MetadataVisitor.VisitDependsOn(new MdDependsOn(SemanticModel, invocation, compositionTypeNames.Select(i => CreateCompositionName(i, _namespace)).ToImmutableArray()));
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
                            var name = GetRequiredConstantValue<string>(nameArgExpression).Trim();
                            var tags = new List<MdTag>(args.Count - 1);
                            for (var index = 1; index < args.Count; index++)
                            {
                                var arg = args[index];
                                tags.Add(new MdTag(index - 1, GetConstantValue<object>(arg.Expression)));
                            }

                            var argType = GetTypeSymbol<ITypeSymbol>(argTypeSyntax);
                            MetadataVisitor.VisitContract(new MdContract(SemanticModel, invocation, argType, tags.ToImmutableArray()));
                            MetadataVisitor.VisitArg(new MdArg(SemanticModel, invocation, argType, name));
                        }

                        break;

                    case nameof(IConfiguration.Root):
                        if (genericName.TypeArgumentList.Arguments is [{ } rootType])
                        {
                            var rootArgs = invocation.ArgumentList.Arguments;
                            var rootSymbol = GetTypeSymbol<INamedTypeSymbol>(rootType);
                            var name = "";
                            if (rootArgs.Count >= 1)
                            {
                                name = GetRequiredConstantValue<string>(rootArgs[0].Expression);
                            }

                            MdTag? tag = default;
                            if (rootArgs.Count >= 2)
                            {
                                tag = new MdTag(0, GetConstantValue<object>(rootArgs[1].Expression));
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

    private static CompositionName CreateCompositionName(string name, string ns)
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

        return new CompositionName(className, newNamespace);
    }
    

    private ImmutableArray<MdUsingDirectives> GetUsingDirectives(SyntaxNode syntaxNode)
    {
        var namespaces = SemanticModel.LookupNamespacesAndTypes(syntaxNode.Span.Start)
            .OfType<INamespaceSymbol>()
            .Where(i => !i.IsGlobalNamespace)
            .Select(i => i.ToString());
        
        var usingDirectives = _usingDirectives
            .Where(i => !i.StaticKeyword.IsKind(SyntaxKind.StaticKeyword))
            .Select(i => i.Name.ToString())
            .Concat(namespaces)
            .Where(i => !string.IsNullOrWhiteSpace(i))
            .Distinct()
            .ToImmutableArray();
        
        var staticUsingDirectives = _usingDirectives
            .Where(i => i.StaticKeyword.IsKind(SyntaxKind.StaticKeyword))
            .Select(i => i.Name.ToString())
            .Where(i => !string.IsNullOrWhiteSpace(i))
            .Distinct()
            .ToImmutableArray();
        
        return ImmutableArray.Create(new MdUsingDirectives(usingDirectives, staticUsingDirectives));
    }

    public override void VisitUsingDirective(UsingDirectiveSyntax usingDirective)
    {
        if (!_namespaces.Any())
        {
            _usingDirectives.Add(usingDirective);
        }

        base.VisitUsingDirective(usingDirective);
    }

    public override void VisitFileScopedNamespaceDeclaration(FileScopedNamespaceDeclarationSyntax namespaceDeclaration)
    {
        _namespace = namespaceDeclaration.Name.ToString().Trim();
        _namespaces.Add(_namespace);
        base.VisitFileScopedNamespaceDeclaration(namespaceDeclaration);
    }

    public override void VisitNamespaceDeclaration(NamespaceDeclarationSyntax namespaceDeclaration)
    {
        _namespace = namespaceDeclaration.Name.ToString().Trim();
        _namespaces.Add(_namespace);
        base.VisitNamespaceDeclaration(namespaceDeclaration);
    }

    private void VisitFactory(ITypeSymbol resultType, SimpleLambdaExpressionSyntax lambdaExpression)
    {
        var resolversWalker = new FactoryResolversSyntaxWalker();
        resolversWalker.Visit(lambdaExpression);
        var position = 0;
        var hasContextTag = false;
        var resolvers = resolversWalker.Select(invocation =>
        {
            if (invocation.ArgumentList.Arguments is { Count: > 0 } arguments)
            {
                switch (arguments)
                {
                    case [{ RefOrOutKeyword.IsMissing: false } targetValue]:
                        if (SemanticModel.GetOperation(arguments[0]) is IArgumentOperation argumentOperation)
                        {
                            return new MdResolver(
                                SemanticModel,
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
                        
                        var resolverTag = new MdTag(0, hasContextTag ? MdTag.ContextTag : GetConstantValue<object>(tag.Expression));
                        if (arguments.Count > 0 && SemanticModel.GetOperation(arguments[1]) is IArgumentOperation argumentOperation2)
                        {
                            return new MdResolver(
                                SemanticModel,
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

        MetadataVisitor.VisitFactory(
            new MdFactory(
                SemanticModel,
                lambdaExpression,
                resultType,
                lambdaExpression,
                lambdaExpression.Parameter,
                resolvers,
                hasContextTag));
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

    private T GetTypeSymbol<T>(SyntaxNode node)
        where T : ITypeSymbol
    {
        var result = TryGetTypeSymbol<T>(node);
        if (result is not null)
        {
            return result;
        }

        _logger.CompileError($"The type {node} is not supported.", node.GetLocation(), LogId.ErrorInvalidMetadata);
        throw HandledException.Shared;
    }
    
    private T? TryGetTypeSymbol<T>(SyntaxNode node)
        where T : ITypeSymbol
    {
        var typeInfo = SemanticModel.GetTypeInfo(node, _cancellationToken);
        var typeSymbol = typeInfo.Type ?? typeInfo.ConvertedType;
        if (typeSymbol is T symbol)
        {
            return (T)symbol.WithNullableAnnotation(NullableAnnotation.NotAnnotated);
        }

        return default;
    }

    // ReSharper disable once SuggestBaseTypeForParameter
    private void NotSupported(InvocationExpressionSyntax invocation)
    {
        _logger.CompileError($"The {invocation} is not supported.", invocation.GetLocation(), LogId.ErrorInvalidMetadata);
        throw HandledException.Shared;
    }

    private T GetRequiredConstantValue<T>(SyntaxNode node)
    {
        var value = GetConstantValue<T>(node);
        if (value is not null)
        {
            return value;
        }
        
        _logger.CompileError($"{node} must be a non-null value of type {typeof(T)}.", node.GetLocation(), LogId.ErrorInvalidMetadata);
        throw HandledException.Shared;
    }

    private T? GetConstantValue<T>(SyntaxNode node)
    {
        switch (node)
        {
            case LiteralExpressionSyntax literalExpression:
            {
                if (literalExpression.IsKind(SyntaxKind.DefaultLiteralExpression)
                    || literalExpression.IsKind(SyntaxKind.NullLiteralExpression))
                {
                    return default;
                }
                
                return (T?)literalExpression.Token.Value;
            }
            
            case MemberAccessExpressionSyntax memberAccessExpressionSyntax 
                when memberAccessExpressionSyntax.IsKind(SyntaxKind.SimpleMemberAccessExpression):
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

                    break;
                }
        }
        
        var optionalValue = SemanticModel.GetConstantValue(node);
        if (optionalValue.Value is not null)
        {
            return (T)optionalValue.Value;
        }

        var operation = SemanticModel.GetOperation(node);
        if (operation?.ConstantValue.Value is not null)
        {
            return (T)operation.ConstantValue.Value!;
        }
        
        if (typeof(T) == typeof(object) && operation is ITypeOfOperation typeOfOperation)
        {
            return (T)typeOfOperation.TypeOperand;
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
            values.Add(GetRequiredConstantValue<T>(argument.Expression));
        }

        return values;
    }

    private ImmutableArray<MdTag>.Builder BuildTags(SeparatedSyntaxList<ArgumentSyntax> arguments)
    {
        var builder = ImmutableArray.CreateBuilder<MdTag>();
        for (var index = 0; index < arguments.Count; index++)
        {
            var argument = arguments[index];
            builder.Add(new MdTag(index, GetConstantValue<object>(argument.Expression)));
        }

        return builder;
    }
    
    private static string GetInvocationName(InvocationExpressionSyntax invocation) => GetName(invocation.Expression, 2);

    private static string GetName(ExpressionSyntax expression, int deepness = int.MaxValue)
    {
        switch (expression)
        {
            case IdentifierNameSyntax identifierNameSyntax:
                return identifierNameSyntax.Identifier.Text;
            
            case MemberAccessExpressionSyntax memberAccess when memberAccess.IsKind(SyntaxKind.SimpleMemberAccessExpression):
            {
                var name = memberAccess.Name.Identifier.Text;
                if (--deepness > 0)
                {
                    var prefix = GetName(memberAccess.Expression, deepness);
                    return prefix == string.Empty ? name : $"{prefix}.{name}";
                }

                return name;
            }
            
            default:
                return string.Empty;
        }
    }
    
    [SuppressMessage("ReSharper", "SuggestBaseTypeForParameter")]
    private static bool IsMetadata(InvocationExpressionSyntax invocation)
    {
        if (!ApiMethods.Contains(GetInvocationName(invocation)))
        {
            return false;
        }

        var setupInvocation = invocation
            .DescendantNodesAndSelf()
            .OfType<InvocationExpressionSyntax>()
            .FirstOrDefault(i => GetInvocationName(i) == DISetup);

        return setupInvocation is not null;
    }
}