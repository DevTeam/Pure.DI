// ReSharper disable InvertIf
// ReSharper disable ConvertIfStatementToSwitchStatement
// ReSharper disable MergeIntoPattern
// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core;

using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.Operations;
using NS35EBD81B;

internal sealed class MetadataWalker : CSharpSyntaxWalker, IMetadataWalker
{
    private static readonly Regex CommentRegex = new(@"//\s*(\w+)\s*=\s*(.+)\s*", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Singleline);
    private readonly IOwnerProvider _ownerProvider;
    private readonly ITargetClassNameProvider _targetClassNameProvider;
    private readonly ISyntaxFilter _syntaxFilter;
    private readonly IDiagnostic _diagnostic;
    private readonly INameService _nameService;
    private readonly IStringTools _stringTools;
    private readonly IArgumentsSupport _argumentsSupport;
    private readonly List<ResolverMetadata> _metadata = new();
    private ResolverMetadata? _resolver;
    private BindingMetadata _binding = new();
    private Lifetime _defaultLifetime = Lifetime.Transient;
    private SemanticModel? _semanticModel;

    public MetadataWalker(
        IOwnerProvider ownerProvider,
        ITargetClassNameProvider targetClassNameProvider,
        [Tag(Tags.MetadataSyntax)] ISyntaxFilter syntaxFilter,
        IDiagnostic diagnostic,
        [Tag(Tags.Default)] INameService nameService,
        IStringTools stringTools,
        IArgumentsSupport argumentsSupport)
    {
        _ownerProvider = ownerProvider;
        _targetClassNameProvider = targetClassNameProvider;
        _syntaxFilter = syntaxFilter;
        _diagnostic = diagnostic;
        _nameService = nameService;
        _stringTools = stringTools;
        _argumentsSupport = argumentsSupport;
    }

    public IEnumerable<ResolverMetadata> Metadata => _metadata;

    private SemanticModel SemanticModel => _semanticModel ?? throw new InvalidOperationException("SemanticModel is not initialized.");

    public MetadataWalker Initialize(SemanticModel semanticModel )
    {
        _semanticModel = semanticModel;
        return this;
    }

    public override void VisitInvocationExpression(InvocationExpressionSyntax node)
    {
        base.VisitInvocationExpression(node);
        if (!_syntaxFilter.Accept(node))
        {
            return;
        }

        var operation = SemanticModel.GetOperation(node);
        if (operation is IInvalidOperation)
        {
            _binding = new BindingMetadata();
            return;
        }

        if (operation?.Type == null || operation is not IInvocationOperation invocationOperation)
        {
            return;
        }

        if (invocationOperation.TargetMethod.IsStatic)
        {
            // Setup("...")
            if (
                invocationOperation.TargetMethod.Parameters.Length == 1
                && !invocationOperation.TargetMethod.IsGenericMethod
                && invocationOperation.TargetMethod.Name == nameof(DI.Setup)
                && new SemanticType(invocationOperation.TargetMethod.ContainingType, SemanticModel).Equals(typeof(DI))
                && new SemanticType(invocationOperation.TargetMethod.ReturnType, SemanticModel).Equals(typeof(IConfiguration))
                && TryGetValue(invocationOperation.Arguments[0].Value, SemanticModel, out var composerTypeName, string.Empty))
            {
                var owner = _ownerProvider.TryGetOwner(node);
                composerTypeName = _targetClassNameProvider.TryGetName(composerTypeName, node, owner) ?? composerTypeName;
                _resolver = _metadata.FirstOrDefault(i => i.ComposerTypeName.Equals(composerTypeName, StringComparison.InvariantCultureIgnoreCase));
                if (_resolver == null)
                {
                    _resolver = new ResolverMetadata(node, composerTypeName, owner);
                    _metadata.Add(_resolver);
                }

                _binding = new BindingMetadata();
                if (node.HasLeadingTrivia)
                {
                    foreach (
                        var match in from trivia in node.GetLeadingTrivia()
                        where trivia.IsKind(SyntaxKind.SingleLineCommentTrivia)
                        select trivia.ToFullString().Trim()
                        into comment
                        select CommentRegex.Match(comment)
                        into match
                        where match.Success
                        select match)
                    {
                        if (Enum.TryParse(match.Groups[1].Value, true, out Setting setting))
                        {
                            _resolver.Settings[setting] = match.Groups[2].Value;
                        }
                    }
                }
            }

            return;
        }

        if (_resolver == null)
        {
            return;
        }

        if (!invocationOperation.IsVirtual)
        {
            return;
        }

        // DependsOn(string baseConfigurationName)
        if (
            !invocationOperation.TargetMethod.IsGenericMethod
            && invocationOperation.TargetMethod.Parameters.Length == 1
            && !invocationOperation.TargetMethod.IsGenericMethod
            && invocationOperation.TargetMethod.Name == nameof(IConfiguration.DependsOn)
            && new SemanticType(invocationOperation.TargetMethod.ContainingType, SemanticModel).Equals(typeof(IConfiguration))
            && new SemanticType(invocationOperation.TargetMethod.ReturnType, SemanticModel).Equals(typeof(IConfiguration))
            && TryGetValue(invocationOperation.Arguments[0].Value, SemanticModel, out var baseConfigurationName, string.Empty)
            && !string.IsNullOrWhiteSpace(baseConfigurationName))
        {
            _resolver?.DependsOn.Add(baseConfigurationName);
        }

        // Default(Lifetime)
        if (
            !invocationOperation.TargetMethod.IsGenericMethod
            && invocationOperation.TargetMethod.Parameters.Length == 1
            && !invocationOperation.TargetMethod.IsGenericMethod
            && invocationOperation.TargetMethod.Name == nameof(IConfiguration.Default)
            && new SemanticType(invocationOperation.TargetMethod.ContainingType, SemanticModel).Equals(typeof(IConfiguration))
            && new SemanticType(invocationOperation.TargetMethod.ReturnType, SemanticModel).Equals(typeof(IConfiguration))
            && TryGetValue(invocationOperation.Arguments[0].Value, SemanticModel, out var defaultLifetime, Lifetime.Transient))
        {
            _defaultLifetime = defaultLifetime;
            _binding.Lifetime = defaultLifetime;
        }

        if (
            invocationOperation.TargetMethod.IsGenericMethod
            && invocationOperation.TargetMethod.TypeArguments.Length == 1)
        {
            if (invocationOperation.TargetMethod.Parameters.Length == 0)
            {
                // To<>()
                if (invocationOperation.TargetMethod.Name == nameof(IBinding.To)
                    && new SemanticType(invocationOperation.TargetMethod.ContainingType, SemanticModel).Equals(typeof(IBinding))
                    && new SemanticType(invocationOperation.TargetMethod.ReturnType, SemanticModel).Equals(typeof(IConfiguration)))
                {
                    _binding.Implementation = new SemanticType(invocationOperation.TargetMethod.TypeArguments[0], SemanticModel);
                    _binding.Location = node.GetLocation();
                    if (CheckTo(node))
                    {
                        _resolver?.Bindings.Add(_binding);
                    }

                    _binding = new BindingMetadata { Lifetime = _defaultLifetime };
                }

                return;
            }

            if (invocationOperation.TargetMethod.Parameters.Length == 1)
            {
                // Bind<>(params object[] tags)
                if (invocationOperation.TargetMethod.Name == nameof(IBinding.Bind)
                    && (new SemanticType(invocationOperation.TargetMethod.ContainingType, SemanticModel).Equals(typeof(IBinding)) || new SemanticType(invocationOperation.TargetMethod.ContainingType, SemanticModel).Equals(typeof(IConfiguration)))
                    && new SemanticType(invocationOperation.TargetMethod.ReturnType, SemanticModel).Equals(typeof(IBinding))
                    && invocationOperation.Arguments[0].ArgumentKind == ArgumentKind.ParamArray
                    && invocationOperation.Arguments[0].Value is IArrayCreationOperation arrayCreationOperation
                    && arrayCreationOperation.Type is IArrayTypeSymbol arrayTypeSymbol
                    && new SemanticType(arrayTypeSymbol.ElementType, SemanticModel).Equals(typeof(object)))
                {
                    var dependencyType = invocationOperation.TargetMethod.TypeArguments[0];
                    var dependency = new SemanticType(dependencyType, SemanticModel);
                    _binding.AddDependencyTags(dependency, (arrayCreationOperation.Initializer?.ElementValues.OfType<IConversionOperation>().Select(i => i.Syntax).OfType<ExpressionSyntax>() ?? Enumerable.Empty<ExpressionSyntax>()).ToArray());
                    _binding.AddDependency(new SemanticType(dependencyType, SemanticModel));
                }

                // To<>(ctx => new ...())
                if (invocationOperation.TargetMethod.Name == nameof(IBinding.To)
                    && new SemanticType(invocationOperation.TargetMethod.ContainingType, SemanticModel).Equals(typeof(IBinding))
                    && new SemanticType(invocationOperation.TargetMethod.ReturnType, SemanticModel).Equals(typeof(IConfiguration)))
                {
                    _binding.Implementation = new SemanticType(invocationOperation.TargetMethod.TypeArguments[0], SemanticModel);
                    if (
                        invocationOperation.Arguments[0].Syntax is ArgumentSyntax factory
                        && factory.Expression is SimpleLambdaExpressionSyntax lambda)
                    {
                        _binding.Factory = lambda;
                    }

                    if (CheckTo(node))
                    {
                        _resolver?.Bindings.Add(_binding);
                    }

                    _binding = new BindingMetadata
                    {
                        Lifetime = _defaultLifetime
                    };
                }
                
                if (invocationOperation.TargetMethod.Parameters.Length == 1
                    && new SemanticType(invocationOperation.TargetMethod.ContainingType, SemanticModel).Equals(typeof(IConfiguration))
                    && new SemanticType(invocationOperation.TargetMethod.ReturnType, SemanticModel).Equals(typeof(IConfiguration)))
                {
                    // || invocationOperation.TargetMethod.TypeArguments[0] is IArrayTypeSymbol
                    var argument0 = invocationOperation.Arguments[0].Value;
                    int argumentPosition;
                    switch (invocationOperation.TargetMethod.Name)
                    {
                        // TagAttribute<>(...)
                        case nameof(IConfiguration.TagAttribute):
                            if (invocationOperation.TargetMethod.TypeArguments[0] is INamedTypeSymbol tagType &&
                                TryGetValue(argument0, SemanticModel, out argumentPosition, 0))
                            {
                                _resolver?.Attributes.Add(new AttributeMetadata(AttributeKind.Tag, tagType, argumentPosition));
                            }

                            break;

                        // TypeAttribute<>(...)
                        case nameof(IConfiguration.TypeAttribute):
                            if (invocationOperation.TargetMethod.TypeArguments[0] is INamedTypeSymbol typeType &&
                                TryGetValue(argument0, SemanticModel, out argumentPosition, 0))
                            {
                                _resolver?.Attributes.Add(new AttributeMetadata(AttributeKind.Type, typeType, argumentPosition));
                            }

                            break;

                        // OrderAttribute<>(...)
                        case nameof(IConfiguration.OrderAttribute):
                            if (invocationOperation.TargetMethod.TypeArguments[0] is INamedTypeSymbol orderType &&
                                TryGetValue(argument0, SemanticModel, out argumentPosition, 0))
                            {
                                _resolver?.Attributes.Add(new AttributeMetadata(AttributeKind.Order, orderType, argumentPosition));
                            }

                            break;

                        // Arg<>(...)
                        case nameof(IConfiguration.Arg):
                            if (invocationOperation.TargetMethod.TypeArguments[0] is INamedTypeSymbol argType &&
                                invocationOperation.Arguments[0].Value is IArrayCreationOperation arrayCreationOperationArg)
                            {
                                var tags = (arrayCreationOperationArg.Initializer?.ElementValues.OfType<IConversionOperation>().Select(i => i.Syntax).OfType<ExpressionSyntax>() ?? Enumerable.Empty<ExpressionSyntax>()).ToArray();
                                var semanticType = new SemanticType(argType, SemanticModel);
                                var baseName = _stringTools.ConvertToTitle(semanticType.Name);
                                var argKey = new MemberKey($"val{baseName}", invocationOperation);
                                var name = _nameService.FindName(argKey);
                                var arg = new ArgumentMetadata(semanticType, name);
                                _binding.Implementation = semanticType;
                                _binding.BindingType = BindingType.Arg;
                                _binding.Lifetime = Lifetime.Transient;
                                _binding.Factory = _argumentsSupport.CreateArgumentFactory(arg);
                                _binding.AddDependency(semanticType);
                                foreach (var tag in tags)
                                {
                                    _binding.AddTags(tag);
                                }
                                
                                _resolver?.Bindings.Add(_binding);
                                _resolver?.Arguments.Add(arg);
                                _binding = new BindingMetadata { Lifetime = _defaultLifetime };
                            }

                            break;
                        
                        // Root<>(...)
                        case nameof(IConfiguration.Root):
                            if (invocationOperation.Arguments[0].Value is IArrayCreationOperation arrayCreationOperationRoot)
                            {
                                var dependencyType = invocationOperation.TargetMethod.TypeArguments[0];
                                _binding.AddDependency(new SemanticType(dependencyType, SemanticModel));
                                _binding.AddTags((arrayCreationOperationRoot.Initializer?.ElementValues.OfType<IConversionOperation>().Select(i => i.Syntax).OfType<ExpressionSyntax>() ?? Enumerable.Empty<ExpressionSyntax>()).ToArray());
                                _binding.Implementation = new SemanticType(dependencyType, SemanticModel);
                                _binding.BindingType = BindingType.Root;
                                _binding.Lifetime = Lifetime.Transient;
                                _binding.Location = node.GetLocation();

                                _resolver?.Bindings.Add(_binding);
                                _binding = new BindingMetadata { Lifetime = _defaultLifetime };
                            }

                            break;
                    }
                }

                return;
            }

            return;
        }

        if (invocationOperation.Arguments.Length == 1
            && !invocationOperation.TargetMethod.IsGenericMethod)
        {
            // As(Lifetime)
            if (invocationOperation.TargetMethod.Name == nameof(IBinding.As)
                && new SemanticType(invocationOperation.TargetMethod.ContainingType, SemanticModel).Equals(typeof(IBinding))
                && new SemanticType(invocationOperation.TargetMethod.ReturnType, SemanticModel).Equals(typeof(IBinding))
                && TryGetValue(invocationOperation.Arguments[0].Value, SemanticModel, out var lifetime, Lifetime.Transient))
            {
                _binding.Lifetime = lifetime;
            }

            // Tags(params object[] tags)
            if (invocationOperation.TargetMethod.Name == nameof(IBinding.Tags)
                && new SemanticType(invocationOperation.TargetMethod.ContainingType, SemanticModel).Equals(typeof(IBinding))
                && new SemanticType(invocationOperation.TargetMethod.ReturnType, SemanticModel).Equals(typeof(IBinding))
                && invocationOperation.Arguments[0].ArgumentKind == ArgumentKind.ParamArray
                && invocationOperation.Arguments[0].Value is IArrayCreationOperation arrayCreationOperation
                && arrayCreationOperation.Type is IArrayTypeSymbol arrayTypeSymbol
                && new SemanticType(arrayTypeSymbol.ElementType, SemanticModel).Equals(typeof(object)))
            {
                _binding.AddTags((arrayCreationOperation.Initializer?.ElementValues.OfType<IConversionOperation>().Select(i => i.Syntax).OfType<ExpressionSyntax>() ?? Enumerable.Empty<ExpressionSyntax>()).ToArray());
            }
        }

        if (invocationOperation.Arguments.Length == 0
            && !invocationOperation.TargetMethod.IsGenericMethod)
        {
            // AnyTag()
            if (invocationOperation.TargetMethod.Name == nameof(IBinding.AnyTag)
                && new SemanticType(invocationOperation.TargetMethod.ContainingType, SemanticModel).Equals(typeof(IBinding))
                && new SemanticType(invocationOperation.TargetMethod.ReturnType, SemanticModel).Equals(typeof(IBinding)))
            {
                _binding.AnyTag = true;
            }
        }
    }

    private static bool TryGetValue<T>(IOperation operation, SemanticModel semanticModel, out T value, T defaultValue)
    {
        var optionalValue = semanticModel.GetConstantValue(operation.Syntax);
        if (optionalValue.HasValue && optionalValue.Value != null)
        {
            value = (T)optionalValue.Value;
            return true;
        }

        if (operation.ConstantValue.HasValue && operation.ConstantValue.Value != null)
        {
            value = (T)operation.ConstantValue.Value;
            return true;
        }

        value = defaultValue;
        return false;
    }

    // ReSharper disable once SuggestBaseTypeForParameter
    private bool CheckTo(InvocationExpressionSyntax invocation)
    {
        if (_binding.Implementation == default)
        {
            return true;
        }

        if (_binding.Implementation.Type is IErrorTypeSymbol)
        {
            return false;
        }

        var notImplemented = _binding.Dependencies
            .Where(dependency => dependency.Type is not IErrorTypeSymbol)
            .Where(dependency => !_binding.Implementation.Implements(dependency.Type))
            .ToList();

        if (notImplemented.Any())
        {
            var lastNode =
                invocation.DescendantNodes().OfType<GenericNameSyntax>().LastOrDefault(i => i.Identifier.Text == nameof(IBinding.To))
                ?? invocation.DescendantNodes().LastOrDefault()
                ?? invocation;
            
            var error = $"{_binding.Implementation} is not inherited from {string.Join(", ", notImplemented.Select(i => i.ToString()))}";
            _diagnostic.Error(Diagnostics.Error.NotInherited, error, new[] { lastNode.GetLocation() }.ToArray());
            throw new HandledException(error);
        }

        return true;
    }
}