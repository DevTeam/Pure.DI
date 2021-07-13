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
        private readonly Func<IBuildStrategy> _buildStrategy;

        public MicrosoftDependencyInjectionLifetimeStrategy(
            IBuildContext buildContext,
            [Tag(Tags.SimpleBuildStrategy)] Func<IBuildStrategy> buildStrategy,
            Lifetime lifetime)
        {
            _buildContext = buildContext;
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
            return SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                _buildStrategy().Build(serviceProviderDependency, instanceType),
                SyntaxFactory.IdentifierName(nameof(ServiceProviderInstance<object>.Value)));
        }
    }
}