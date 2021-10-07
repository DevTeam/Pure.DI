// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core
{
    using System.Collections.Immutable;
    using System.Linq;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal class CompositeWrapperStrategy: IWrapperStrategy
    {
        private readonly ImmutableArray<IWrapperStrategy> _strategies;

        public CompositeWrapperStrategy(
            [Tag(Tags.FactoryMethod)] IWrapperStrategy factoryMethodWrapperStrategy,
            [Tag(Tags.Factory)] IWrapperStrategy factoryWrapperStrategy) =>
            _strategies = ImmutableArray.Create(factoryMethodWrapperStrategy, factoryWrapperStrategy);

        public ExpressionSyntax Build(SemanticType resolvingType, Dependency dependency, ExpressionSyntax objectBuildExpression) =>
            _strategies.Aggregate(objectBuildExpression, (current, wrapperStrategy) => wrapperStrategy.Build(resolvingType, dependency, current));
    }
}