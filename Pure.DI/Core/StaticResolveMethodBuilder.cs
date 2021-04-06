namespace Pure.DI.Core
{
    using Microsoft.CodeAnalysis.CSharp;

    // ReSharper disable once ClassNeverInstantiated.Global
    internal class StaticResolveMethodBuilder : IResolveMethodBuilder
    {
        private readonly ResolverMetadata _metadata;
        private readonly IFallbackStrategy _fallbackStrategy;
        private readonly IBindingExpressionStrategy _bindingExpressionStrategy;
        private readonly IBindingStatementsStrategy _bindingStatementsStrategy;

        public StaticResolveMethodBuilder(
            ResolverMetadata metadata,
            IFallbackStrategy fallbackStrategy,
            [Tag(Tags.SimpleExpressionStrategy)] IBindingExpressionStrategy bindingExpressionStrategy,
            [Tag(Tags.TypeStatementsStrategy)] IBindingStatementsStrategy bindingStatementsStrategy)
        {
            _metadata = metadata;
            _fallbackStrategy = fallbackStrategy;
            _bindingExpressionStrategy = bindingExpressionStrategy;
            _bindingStatementsStrategy = bindingStatementsStrategy;
        }

        public ResolveMethod Build()
        {
            var returnDefault = _fallbackStrategy.Build(_metadata.Fallback, null, SyntaxFactory.ParseTypeName("type"), SyntaxFactory.DefaultExpression(SyntaxRepo.ObjectTypeSyntax));
            return new ResolveMethod(SyntaxRepo.StaticResolveMethodSyntax, true, _bindingExpressionStrategy, _bindingStatementsStrategy, SyntaxFactory.ParseName("type"), returnDefault);
        }
    }
}