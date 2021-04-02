namespace Pure.DI.Core
{
    using System;
    using System.Collections.Generic;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Operations;

    internal class MetadataWalker: CSharpSyntaxWalker
    {
        private readonly SemanticModel _semanticModel;
        private readonly List<ResolverMetadata> _metadata = new();
        private ResolverMetadata? _resolver;
        private BindingMetadata _binding = new();

        public MetadataWalker(SemanticModel semanticModel) =>
            _semanticModel = semanticModel ?? throw new ArgumentNullException(nameof(semanticModel));

        public IReadOnlyCollection<ResolverMetadata> Metadata => _metadata;

        public override void VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            base.VisitInvocationExpression(node);

            var operation = _semanticModel.GetOperation(node);
            if (operation?.Type == null || !(operation is IInvocationOperation invocationOperation))
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
                    && typeof(DI).Equals(invocationOperation.TargetMethod.ContainingType, _semanticModel)
                    && typeof(IConfiguration).Equals(invocationOperation.TargetMethod.ReturnType, _semanticModel)
                    && TryGetValue(invocationOperation.Arguments[0], _semanticModel, out var targetTypeName, string.Empty))
                {
                    _resolver = new ResolverMetadata(node, targetTypeName);
                    _metadata.Add(_resolver);
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

            if (
                invocationOperation.TargetMethod.IsGenericMethod
                && invocationOperation.TargetMethod.TypeArguments.Length == 1)
            {
                if (invocationOperation.TargetMethod.Parameters.Length == 0)
                {
                    // To<>()
                    if (invocationOperation.TargetMethod.Name == nameof(IBinding.To)
                        && typeof(IBinding).Equals(invocationOperation.TargetMethod.ContainingType, _semanticModel)
                        && typeof(IConfiguration).Equals(invocationOperation.TargetMethod.ReturnType, _semanticModel)
                        && invocationOperation.TargetMethod.TypeArguments[0] is INamedTypeSymbol implementationType)
                    {
                        _binding.ImplementationType = implementationType;
                        _resolver.Bindings.Add(_binding);
                        _binding = new BindingMetadata();
                    }

                    // Bind<>()
                    if (invocationOperation.TargetMethod.Name == nameof(IBinding.Bind)
                        && (typeof(IBinding).Equals(invocationOperation.TargetMethod.ContainingType, _semanticModel) || typeof(IConfiguration).Equals(invocationOperation.TargetMethod.ContainingType, _semanticModel))
                        && typeof(IBinding).Equals(invocationOperation.TargetMethod.ReturnType, _semanticModel)
                        && invocationOperation.TargetMethod.TypeArguments[0] is INamedTypeSymbol contractType)
                    {
                        _binding.ContractTypes.Add(contractType);
                    }
                }
                else
                {
                    // To<>(ctx => new ...())
                    if (invocationOperation.TargetMethod.Name == nameof(IBinding.To)
                        && typeof(IBinding).Equals(invocationOperation.TargetMethod.ContainingType, _semanticModel)
                        && typeof(IConfiguration).Equals(invocationOperation.TargetMethod.ReturnType, _semanticModel)
                        && invocationOperation.TargetMethod.TypeArguments[0] is INamedTypeSymbol implementationType)
                    {
                        _binding.ImplementationType = implementationType;
                        if (
                            invocationOperation.Arguments[0].Syntax is ArgumentSyntax factory
                            && factory.Expression is SimpleLambdaExpressionSyntax lambda)
                        {
                            _binding.Factory = lambda;
                        }

                        _resolver.Bindings.Add(_binding);
                        _binding = new BindingMetadata();
                    }
                }

                return;
            }

            if (invocationOperation.Arguments.Length == 1
                && !invocationOperation.TargetMethod.IsGenericMethod)
            {
                // As(Lifitime)
                if (invocationOperation.TargetMethod.Name == nameof(IBinding.As)
                    && typeof(IBinding).Equals(invocationOperation.TargetMethod.ContainingType, _semanticModel)
                    && typeof(IBinding).Equals(invocationOperation.TargetMethod.ReturnType, _semanticModel)
                    && TryGetValue(invocationOperation.Arguments[0], _semanticModel, out var lifetime, Lifetime.Transient))
                {
                    _binding.Lifetime = lifetime;
                }

                // Tag(object tag)
                if (invocationOperation.TargetMethod.Name == nameof(IBinding.Tag)
                    && typeof(IBinding).Equals(invocationOperation.TargetMethod.ContainingType, _semanticModel)
                    && typeof(IBinding).Equals(invocationOperation.TargetMethod.ReturnType, _semanticModel)
                    && invocationOperation.Arguments[0].Syntax is ArgumentSyntax tag)
                {
                    _binding.Tags.Add(tag.Expression);
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
