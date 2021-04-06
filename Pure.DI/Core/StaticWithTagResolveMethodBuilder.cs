namespace Pure.DI.Core
{
    using Microsoft.CodeAnalysis.CSharp;

    // ReSharper disable once ClassNeverInstantiated.Global
    internal class StaticWithTagResolveMethodBuilder : IResolveMethodBuilder
    {
        private readonly ResolverMetadata _metadata;
        private readonly IFallbackStrategy _fallbackStrategy;
        private readonly IBindingExpressionStrategy _bindingExpressionStrategy;
        private readonly IBindingStatementsStrategy _bindingStatementsStrategy;

        public StaticWithTagResolveMethodBuilder(
            ResolverMetadata metadata,
            IFallbackStrategy fallbackStrategy,
            [Tag(Tags.SimpleExpressionStrategy)] IBindingExpressionStrategy bindingExpressionStrategy,
            [Tag(Tags.TypeAndTagStatementsStrategy)] IBindingStatementsStrategy bindingStatementsStrategy)
        {
            _metadata = metadata;
            _fallbackStrategy = fallbackStrategy;
            _bindingExpressionStrategy = bindingExpressionStrategy;
            _bindingStatementsStrategy = bindingStatementsStrategy;
        }

        public ResolveMethod Build()
        {
            var returnWithTagDefault = _fallbackStrategy.Build(_metadata.Fallback, null, SyntaxFactory.ParseTypeName("type"), SyntaxFactory.ParseTypeName("type"));
            return new ResolveMethod(SyntaxRepo.StaticResolveWithTagMethodSyntax, false, _bindingExpressionStrategy, _bindingStatementsStrategy, SyntaxFactory.ParseName("type"), returnWithTagDefault);
        }
    }
}