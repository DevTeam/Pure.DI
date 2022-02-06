// ReSharper disable InvertIf
// ReSharper disable ConvertIfStatementToSwitchStatement
// ReSharper disable MergeIntoPattern
namespace Pure.DI.Core;

using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.Operations;
using NS35EBD81B;

internal class MetadataWalker : CSharpSyntaxWalker, IMetadataWalker
{
    private static readonly Regex CommentRegex = new(@"//\s*(\w+)\s*=\s*(.+)\s*", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Singleline);
    private readonly SemanticModel _semanticModel;
    private readonly IOwnerProvider _ownerProvider;
    private readonly ITargetClassNameProvider _targetClassNameProvider;
    private readonly ISyntaxFilter _syntaxFilter;
    private readonly List<ResolverMetadata> _metadata = new();
    private ResolverMetadata? _resolver;
    private BindingMetadata _binding = new();
    private Lifetime _defaultLifetime = Lifetime.Transient;

    public MetadataWalker(
        SemanticModel semanticModel,
        IOwnerProvider ownerProvider,
        ITargetClassNameProvider targetClassNameProvider,
        ISyntaxFilter syntaxFilter)
    {
        _semanticModel = semanticModel ?? throw new ArgumentNullException(nameof(semanticModel));
        _ownerProvider = ownerProvider;
        _targetClassNameProvider = targetClassNameProvider;
        _syntaxFilter = syntaxFilter;
    }

    public IEnumerable<ResolverMetadata> Metadata => _metadata;

    public override void VisitInvocationExpression(InvocationExpressionSyntax node)
    {
        base.VisitInvocationExpression(node);
        if (!_syntaxFilter.Accept(node))
        {
            return;
        }

        var operation = _semanticModel.GetOperation(node);
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
                && new SemanticType(invocationOperation.TargetMethod.ContainingType, _semanticModel).Equals(typeof(DI))
                && new SemanticType(invocationOperation.TargetMethod.ReturnType, _semanticModel).Equals(typeof(IConfiguration))
                && TryGetValue(invocationOperation.Arguments[0].Value, _semanticModel, out var composerTypeName, string.Empty))
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
            && new SemanticType(invocationOperation.TargetMethod.ContainingType, _semanticModel).Equals(typeof(IConfiguration))
            && new SemanticType(invocationOperation.TargetMethod.ReturnType, _semanticModel).Equals(typeof(IConfiguration))
            && TryGetValue(invocationOperation.Arguments[0].Value, _semanticModel, out var baseConfigurationName, string.Empty)
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
            && new SemanticType(invocationOperation.TargetMethod.ContainingType, _semanticModel).Equals(typeof(IConfiguration))
            && new SemanticType(invocationOperation.TargetMethod.ReturnType, _semanticModel).Equals(typeof(IConfiguration))
            && TryGetValue(invocationOperation.Arguments[0].Value, _semanticModel, out var defaultLifetime, Lifetime.Transient))
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
                    && new SemanticType(invocationOperation.TargetMethod.ContainingType, _semanticModel).Equals(typeof(IBinding))
                    && new SemanticType(invocationOperation.TargetMethod.ReturnType, _semanticModel).Equals(typeof(IConfiguration)))
                {
                    _binding.Implementation = new SemanticType(invocationOperation.TargetMethod.TypeArguments[0], _semanticModel);
                    _binding.Location = node.GetLocation();
                    _resolver?.Bindings.Add(_binding);
                    _binding = new BindingMetadata
                    {
                        Lifetime = _defaultLifetime
                    };
                }

                return;
            }

            if (invocationOperation.TargetMethod.Parameters.Length == 1)
            {
                // Bind<>(params object[] tags)
                if (invocationOperation.TargetMethod.Name == nameof(IBinding.Bind)
                    && (new SemanticType(invocationOperation.TargetMethod.ContainingType, _semanticModel).Equals(typeof(IBinding)) || new SemanticType(invocationOperation.TargetMethod.ContainingType, _semanticModel).Equals(typeof(IConfiguration)))
                    && new SemanticType(invocationOperation.TargetMethod.ReturnType, _semanticModel).Equals(typeof(IBinding))
                    && invocationOperation.Arguments[0].ArgumentKind == ArgumentKind.ParamArray
                    && invocationOperation.Arguments[0].Value is IArrayCreationOperation arrayCreationOperation
                    && arrayCreationOperation.Type is IArrayTypeSymbol arrayTypeSymbol
                    && new SemanticType(arrayTypeSymbol.ElementType, _semanticModel).Equals(typeof(object)))
                {
                    var dependencyType = invocationOperation.TargetMethod.TypeArguments[0];
                    var dependency = new SemanticType(dependencyType, _semanticModel);
                    _binding.AddDependencyTags(dependency, (arrayCreationOperation.Initializer?.ElementValues.OfType<IConversionOperation>().Select(i => i.Syntax).OfType<ExpressionSyntax>() ?? Enumerable.Empty<ExpressionSyntax>()).ToArray());
                    _binding.AddDependency(new SemanticType(dependencyType, _semanticModel));
                }

                // To<>(ctx => new ...())
                if (invocationOperation.TargetMethod.Name == nameof(IBinding.To)
                    && new SemanticType(invocationOperation.TargetMethod.ContainingType, _semanticModel).Equals(typeof(IBinding))
                    && new SemanticType(invocationOperation.TargetMethod.ReturnType, _semanticModel).Equals(typeof(IConfiguration)))
                {
                    _binding.Implementation = new SemanticType(invocationOperation.TargetMethod.TypeArguments[0], _semanticModel);
                    if (
                        invocationOperation.Arguments[0].Syntax is ArgumentSyntax factory
                        && factory.Expression is SimpleLambdaExpressionSyntax lambda)
                    {
                        _binding.Factory = lambda;
                    }

                    _resolver?.Bindings.Add(_binding);
                    _binding = new BindingMetadata
                    {
                        Lifetime = _defaultLifetime
                    };
                }

                // TagAttribute<>(...)
                if (invocationOperation.TargetMethod.Parameters.Length == 1
                    && new SemanticType(invocationOperation.TargetMethod.ContainingType, _semanticModel).Equals(typeof(IConfiguration))
                    && new SemanticType(invocationOperation.TargetMethod.ReturnType, _semanticModel).Equals(typeof(IConfiguration))
                    && invocationOperation.TargetMethod.TypeArguments[0] is INamedTypeSymbol attributeType
                    && TryGetValue(invocationOperation.Arguments[0].Value, _semanticModel, out var argumentPosition, 0))
                {
                    AttributeKind? attributeKind = invocationOperation.TargetMethod.Name switch
                    {
                        nameof(IConfiguration.TagAttribute) => AttributeKind.Tag,
                        nameof(IConfiguration.TypeAttribute) => AttributeKind.Type,
                        nameof(IConfiguration.OrderAttribute) => AttributeKind.Order,
                        _ => null
                    };

                    if (attributeKind != null)
                    {
                        _resolver?.Attributes.Add(new AttributeMetadata((AttributeKind)attributeKind, attributeType, argumentPosition));
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
                && new SemanticType(invocationOperation.TargetMethod.ContainingType, _semanticModel).Equals(typeof(IBinding))
                && new SemanticType(invocationOperation.TargetMethod.ReturnType, _semanticModel).Equals(typeof(IBinding))
                && TryGetValue(invocationOperation.Arguments[0].Value, _semanticModel, out var lifetime, Lifetime.Transient))
            {
                _binding.Lifetime = lifetime;
            }

            // Tags(params object[] tags)
            if (invocationOperation.TargetMethod.Name == nameof(IBinding.Tags)
                && new SemanticType(invocationOperation.TargetMethod.ContainingType, _semanticModel).Equals(typeof(IBinding))
                && new SemanticType(invocationOperation.TargetMethod.ReturnType, _semanticModel).Equals(typeof(IBinding))
                && invocationOperation.Arguments[0].ArgumentKind == ArgumentKind.ParamArray
                && invocationOperation.Arguments[0].Value is IArrayCreationOperation arrayCreationOperation
                && arrayCreationOperation.Type is IArrayTypeSymbol arrayTypeSymbol
                && new SemanticType(arrayTypeSymbol.ElementType, _semanticModel).Equals(typeof(object)))
            {
                _binding.AddTags((arrayCreationOperation.Initializer?.ElementValues.OfType<IConversionOperation>().Select(i => i.Syntax).OfType<ExpressionSyntax>() ?? Enumerable.Empty<ExpressionSyntax>()).ToArray());
            }
        }

        if (invocationOperation.Arguments.Length == 0
            && !invocationOperation.TargetMethod.IsGenericMethod)
        {
            // AnyTag()
            if (invocationOperation.TargetMethod.Name == nameof(IBinding.AnyTag)
                && new SemanticType(invocationOperation.TargetMethod.ContainingType, _semanticModel).Equals(typeof(IBinding))
                && new SemanticType(invocationOperation.TargetMethod.ReturnType, _semanticModel).Equals(typeof(IBinding)))
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
}