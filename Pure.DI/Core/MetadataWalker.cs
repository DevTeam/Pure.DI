namespace Pure.DI.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Operations;

    internal class MetadataWalker: CSharpSyntaxWalker
    {
        private readonly SemanticModel _semanticModel;
        private readonly List<ResolverMetadata> _metadata = new List<ResolverMetadata>();
        private readonly List<UsingDirectiveSyntax> _usingDirectives = new List<UsingDirectiveSyntax>();
        private string _namespace;
        private ResolverMetadata _resolver;
        private BindingMetadata _binding = new BindingMetadata();

        public MetadataWalker(SemanticModel semanticModel) =>
            _semanticModel = semanticModel ?? throw new ArgumentNullException(nameof(semanticModel));

        public IReadOnlyCollection<ResolverMetadata> Metadata => _metadata;

        public override void VisitNamespaceDeclaration(NamespaceDeclarationSyntax node)
        {
            _namespace = node.Name.ToString();
            base.VisitNamespaceDeclaration(node);
        }

        public override void VisitUsingDirective(UsingDirectiveSyntax node)
        {
            _usingDirectives.Add(node);
            base.VisitUsingDirective(node);
        }

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
                    && typeof(IConfiguration).Equals(invocationOperation.TargetMethod.ReturnType, _semanticModel))
                {
                    var targetTypeName = GetValue<string>(invocationOperation.Arguments[0], _semanticModel);
                    _resolver = new ResolverMetadata(node, targetTypeName);
                    _metadata.Add(_resolver);
                    
                    _namespace = null;
                    _usingDirectives.Clear();
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
                // As(Lifitime lifitime)
                if (invocationOperation.TargetMethod.Name == nameof(IBinding.As)
                    && typeof(IBinding).Equals(invocationOperation.TargetMethod.ContainingType, _semanticModel)
                    && typeof(IBinding).Equals(invocationOperation.TargetMethod.ReturnType, _semanticModel))
                {
                    _binding.Lifetime = GetValue<Lifetime>(invocationOperation.Arguments[0], _semanticModel);
                }

                // Tag(object tag)
                if (invocationOperation.TargetMethod.Name == nameof(IBinding.Tag)
                    && typeof(IBinding).Equals(invocationOperation.TargetMethod.ContainingType, _semanticModel)
                    && typeof(IBinding).Equals(invocationOperation.TargetMethod.ReturnType, _semanticModel))
                {
                    if (invocationOperation.Arguments[0].Syntax is ArgumentSyntax tag)
                    {
                        _binding.Tags.Add(tag.Expression);
                    }
                }
            }

        }

        private static T GetValue<T>(IArgumentOperation arg, SemanticModel semanticModel)
        {
            var value = semanticModel.GetConstantValue(arg.Value.Syntax);
            if (value.HasValue)
            {
                return (T)value.Value;
            }

            if (arg.Value.ConstantValue.HasValue)
            {
                return (T)arg.Value.ConstantValue.Value;
            }

            return default;
        }
    }
}
