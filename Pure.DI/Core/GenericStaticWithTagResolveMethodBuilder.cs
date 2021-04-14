namespace Pure.DI.Core
{
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;

    // ReSharper disable once ClassNeverInstantiated.Global
    internal class GenericStaticWithTagResolveMethodBuilder : IResolveMethodBuilder
    {
        private readonly IFallbackStrategy _fallbackStrategy;
        private readonly IBuildStrategy _buildStrategy;
        private readonly IBindingStatementsStrategy _bindingStatementsStrategy;

        public GenericStaticWithTagResolveMethodBuilder(
            IFallbackStrategy fallbackStrategy,
            [Tag(Tags.GenericBuildStrategy)] IBuildStrategy buildStrategy,
            [Tag(Tags.TypeAndTagStatementsStrategy)] IBindingStatementsStrategy bindingStatementsStrategy)
        {
            _fallbackStrategy = fallbackStrategy;
            _buildStrategy = buildStrategy;
            _bindingStatementsStrategy = bindingStatementsStrategy;
        }

        public ResolveMethod Build(SemanticModel semanticModel)
        {
            var genericWithTagReturnDefault = _fallbackStrategy.Build(semanticModel, SyntaxRepo.TTypeSyntax, SyntaxFactory.TypeOfExpression(SyntaxRepo.TTypeSyntax), SyntaxFactory.ParseTypeName("tag"));
            return new ResolveMethod(SyntaxRepo.GenericStaticResolveWithTagMethodSyntax, false, _buildStrategy, _bindingStatementsStrategy, SyntaxFactory.TypeOfExpression(SyntaxRepo.TTypeSyntax), genericWithTagReturnDefault);
        }
    }
}