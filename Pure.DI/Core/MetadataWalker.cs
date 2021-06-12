// ReSharper disable InvertIf
// ReSharper disable ConvertIfStatementToSwitchStatement
// ReSharper disable MergeIntoPattern
namespace Pure.DI.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Operations;

    internal class MetadataWalker: CSharpSyntaxWalker, IMetadataWalker
    {
        private static readonly Regex CommentRegex = new(@"//\s*(\w+)\s*=\s*(.+)\s*", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Singleline);
        private readonly SemanticModel _semanticModel;
        private readonly IOwnerProvider _ownerProvider;
        private readonly ITargetClassNameProvider _targetClassNameProvider;
        private readonly List<ResolverMetadata> _metadata = new();
        private ResolverMetadata? _resolver;
        private BindingMetadata _binding = new();
        private Lifetime _defaultLifetime = Lifetime.Transient;

        public MetadataWalker(
            SemanticModel semanticModel,
            IOwnerProvider ownerProvider,
            ITargetClassNameProvider targetClassNameProvider)
        {
            _semanticModel = semanticModel ?? throw new ArgumentNullException(nameof(semanticModel));
            _ownerProvider = ownerProvider;
            _targetClassNameProvider = targetClassNameProvider;
        }

        public IEnumerable<ResolverMetadata> Metadata => _metadata;

        public override void VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            base.VisitInvocationExpression(node);

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
                    && TryGetValue(invocationOperation.Arguments[0], _semanticModel, out var targetTypeName, string.Empty))
                {
                    var owner = _ownerProvider.TryGetOwner(node);
                    targetTypeName = _targetClassNameProvider.TryGetName(targetTypeName, node, owner) ?? targetTypeName;
                    _resolver = _metadata.FirstOrDefault(i => i.TargetTypeName.Equals(targetTypeName, StringComparison.InvariantCultureIgnoreCase));
                    if (_resolver == null)
                    {
                        _resolver = new ResolverMetadata(node, targetTypeName, owner);
                        _metadata.Add(_resolver);
                    }

                    _binding = new BindingMetadata();

                    if (node.HasLeadingTrivia)
                    {
                        foreach (
                            var match in from trivia in node.GetLeadingTrivia()
                            where trivia.Kind() == SyntaxKind.SingleLineCommentTrivia
                            select trivia.ToFullString().Trim() into comment
                            select CommentRegex.Match(comment) into match 
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

            // DependsOn("...")
            if (
                !invocationOperation.TargetMethod.IsGenericMethod
                && invocationOperation.TargetMethod.Parameters.Length == 1
                && !invocationOperation.TargetMethod.IsGenericMethod
                && invocationOperation.TargetMethod.Name == nameof(IConfiguration.DependsOn)
                && new SemanticType(invocationOperation.TargetMethod.ContainingType, _semanticModel).Equals(typeof(IConfiguration))
                && new SemanticType(invocationOperation.TargetMethod.ReturnType, _semanticModel).Equals(typeof(IConfiguration))
                && TryGetValue(invocationOperation.Arguments[0], _semanticModel, out var dependencyName, string.Empty)
                && !string.IsNullOrWhiteSpace(dependencyName))
            {
                _resolver?.DependsOn.Add(dependencyName);
            }
            
            // Default(Lifetime)
            if (
                !invocationOperation.TargetMethod.IsGenericMethod
                && invocationOperation.TargetMethod.Parameters.Length == 1
                && !invocationOperation.TargetMethod.IsGenericMethod
                && invocationOperation.TargetMethod.Name == nameof(IConfiguration.Default)
                && new SemanticType(invocationOperation.TargetMethod.ContainingType, _semanticModel).Equals(typeof(IConfiguration))
                && new SemanticType(invocationOperation.TargetMethod.ReturnType, _semanticModel).Equals(typeof(IConfiguration))
                && TryGetValue(invocationOperation.Arguments[0], _semanticModel, out var defaultLifetime, Lifetime.Transient))
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
                        && new SemanticType(invocationOperation.TargetMethod.ReturnType, _semanticModel).Equals(typeof(IConfiguration))
                        && invocationOperation.TargetMethod.TypeArguments[0] is INamedTypeSymbol implementationType)
                    {
                        _binding.Implementation = new SemanticType(implementationType, _semanticModel);
                        _binding.Location = node.GetLocation();
                        _resolver?.Bindings.Add(_binding);
                        _binding = new BindingMetadata
                        {
                            Lifetime = _defaultLifetime
                        };
                    }

                    // Bind<>()
                    if (invocationOperation.TargetMethod.Name == nameof(IBinding.Bind)
                        && (new SemanticType(invocationOperation.TargetMethod.ContainingType, _semanticModel).Equals(typeof(IBinding)) || new SemanticType(invocationOperation.TargetMethod.ContainingType, _semanticModel).Equals(typeof(IConfiguration)))
                        && new SemanticType(invocationOperation.TargetMethod.ReturnType, _semanticModel).Equals(typeof(IBinding))
                        && invocationOperation.TargetMethod.TypeArguments[0] is INamedTypeSymbol dependencyType)
                    {
                        _binding.Dependencies.Add(new SemanticType(dependencyType, _semanticModel));
                    }

                    return;
                }
                
                if (invocationOperation.TargetMethod.Parameters.Length == 1)
                {
                    // To<>(ctx => new ...())
                    if (invocationOperation.TargetMethod.Name == nameof(IBinding.To)
                        && new SemanticType(invocationOperation.TargetMethod.ContainingType, _semanticModel).Equals(typeof(IBinding))
                        && new SemanticType(invocationOperation.TargetMethod.ReturnType, _semanticModel).Equals(typeof(IConfiguration))
                        && invocationOperation.TargetMethod.TypeArguments[0] is INamedTypeSymbol implementationType)
                    {
                        _binding.Implementation = new SemanticType(implementationType, _semanticModel);
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
                        && TryGetValue(invocationOperation.Arguments[0], _semanticModel, out var argumentPosition, 0))
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
                    && TryGetValue(invocationOperation.Arguments[0], _semanticModel, out var lifetime, Lifetime.Transient))
                {
                    _binding.Lifetime = lifetime;
                }

                // Tag(object tag)
                if (invocationOperation.TargetMethod.Name == nameof(IBinding.Tag)
                    && new SemanticType(invocationOperation.TargetMethod.ContainingType, _semanticModel).Equals(typeof(IBinding))
                    && new SemanticType(invocationOperation.TargetMethod.ReturnType, _semanticModel).Equals(typeof(IBinding))
                    && invocationOperation.Arguments[0].Syntax is ArgumentSyntax tag)
                {
                    _binding.Tags.Add(tag.Expression);
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

        private static bool TryGetValue<T>(IArgumentOperation arg, SemanticModel semanticModel, out T value, T defaultValue)
        {
            var optionalValue = semanticModel.GetConstantValue(arg.Value.Syntax);
            if (optionalValue.HasValue && optionalValue.Value != null)
            {
                value = (T)optionalValue.Value;
                return true;
            }

            if (arg.Value.ConstantValue.HasValue && arg.Value.ConstantValue.Value != null)
            {
                value = (T)arg.Value.ConstantValue.Value;
                return true;
            }

            value = defaultValue;
            return false;
        }
    }
}
