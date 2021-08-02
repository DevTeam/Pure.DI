// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core
{
    using System;
    using System.Linq;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal class MicrosoftDependencyInjectionLifetimeStrategy: ILifetimeStrategy
    {
        private readonly IBuildContext _buildContext;
        private readonly IDiagnostic _diagnostic;
        private readonly Func<IBuildStrategy> _buildStrategy;

        public MicrosoftDependencyInjectionLifetimeStrategy(
            IBuildContext buildContext,
            IDiagnostic diagnostic,
            Func<IBuildStrategy> buildStrategy,
            Lifetime lifetime)
        {
            _buildContext = buildContext;
            _diagnostic = diagnostic;
            _buildStrategy = buildStrategy;
            Lifetime = lifetime;
        }

        // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Global
        public Lifetime Lifetime { get; set; }
        
        public ExpressionSyntax Build(Dependency dependency, ExpressionSyntax objectBuildExpression)
        {
            var type = dependency.Binding.Dependencies.FirstOrDefault() ?? dependency.Implementation;
            var serviceProviderInstance = new SemanticType(dependency.Implementation.SemanticModel.Compilation.GetTypeByMetadataName("Pure.DI.ServiceProviderInstance`1")!, dependency.Implementation.SemanticModel);
            var instanceType = serviceProviderInstance.Construct(type);
            var serviceProviderDependency = _buildContext.TypeResolver.Resolve(instanceType, dependency.Tag, dependency.Implementation.Type.Locations);
            var serviceProvider = _buildStrategy().TryBuild(serviceProviderDependency, instanceType);
            if (serviceProvider == null)
            {
                var error = $"Cannot resolve {serviceProviderDependency}.";
                _diagnostic.Error(Diagnostics.Error.CannotResolve, error);
                throw new HandledException(error);
            }
            return SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                serviceProvider,
                SyntaxFactory.IdentifierName(nameof(ServiceProviderInstance<object>.Value)));
        }
    }
}