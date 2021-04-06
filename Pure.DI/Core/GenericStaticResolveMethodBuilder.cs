namespace Pure.DI.Core
{
    using Microsoft.CodeAnalysis.CSharp;

    // ReSharper disable once ClassNeverInstantiated.Global
    internal class GenericStaticResolveMethodBuilder : IResolveMethodBuilder
    {
        private readonly ResolverMetadata _metadata;
        private readonly IFallbackStrategy _fallbackStrategy;
        private readonly IBindingExpressionStrategy _bindingExpressionStrategy;
        private readonly IBindingStatementsStrategy _bindingStatementsStrategy;

        public GenericStaticResolveMethodBuilder(
            ResolverMetadata metadata,
            IFallbackStrategy fallbackStrategy,
            [Tag(Tags.GenericExpressionStrategy)] IBindingExpressionStrategy bindingExpressionStrategy,
            [Tag(Tags.TypeStatementsStrategy)] IBindingStatementsStrategy bindingStatementsStrategy)
        {
            _metadata = metadata;
            _fallbackStrategy = fallbackStrategy;
            _bindingExpressionStrategy = bindingExpressionStrategy;
            _bindingStatementsStrategy = bindingStatementsStrategy;
        }

        public ResolveMethod Build()
        {
            var genericReturnDefault = _fallbackStrategy.Build(_metadata.Fallback, SyntaxRepo.TTypeSyntax, SyntaxFactory.TypeOfExpression(SyntaxRepo.TTypeSyntax), SyntaxFactory.DefaultExpression(SyntaxRepo.ObjectTypeSyntax));
            return new ResolveMethod(SyntaxRepo.GenericStaticResolveMethodSyntax, true, _bindingExpressionStrategy, _bindingStatementsStrategy, SyntaxFactory.TypeOfExpression(SyntaxRepo.TTypeSyntax), genericReturnDefault);
        }
    }
}
