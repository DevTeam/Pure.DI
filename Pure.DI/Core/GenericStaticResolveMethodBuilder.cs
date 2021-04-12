namespace Pure.DI.Core
{
    using Microsoft.CodeAnalysis.CSharp;

    // ReSharper disable once ClassNeverInstantiated.Global
    internal class GenericStaticResolveMethodBuilder : IResolveMethodBuilder
    {
        private readonly ResolverMetadata _metadata;
        private readonly IFallbackStrategy _fallbackStrategy;
        private readonly IBuildStrategy _buildStrategy;
        private readonly IBindingStatementsStrategy _bindingStatementsStrategy;

        public GenericStaticResolveMethodBuilder(
            ResolverMetadata metadata,
            IFallbackStrategy fallbackStrategy,
            [Tag(Tags.GenericBuildStrategy)] IBuildStrategy buildStrategy,
            [Tag(Tags.TypeStatementsStrategy)] IBindingStatementsStrategy bindingStatementsStrategy)
        {
            _metadata = metadata;
            _fallbackStrategy = fallbackStrategy;
            _buildStrategy = buildStrategy;
            _bindingStatementsStrategy = bindingStatementsStrategy;
        }

        public ResolveMethod Build()
        {
            var genericReturnDefault = _fallbackStrategy.Build(_metadata.Fallback, SyntaxRepo.TTypeSyntax, SyntaxFactory.TypeOfExpression(SyntaxRepo.TTypeSyntax), SyntaxFactory.DefaultExpression(SyntaxRepo.ObjectTypeSyntax));
            return new ResolveMethod(SyntaxRepo.GenericStaticResolveMethodSyntax, true, _buildStrategy, _bindingStatementsStrategy, SyntaxFactory.TypeOfExpression(SyntaxRepo.TTypeSyntax), genericReturnDefault);
        }
    }
}
