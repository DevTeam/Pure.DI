namespace Pure.DI.Core
{
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;

    // ReSharper disable once ClassNeverInstantiated.Global
    internal class StaticResolveMethodBuilder : IResolveMethodBuilder
    {
        private readonly IFallbackStrategy _fallbackStrategy;
        private readonly IBuildStrategy _buildStrategy;
        private readonly IBindingStatementsStrategy _bindingStatementsStrategy;

        public StaticResolveMethodBuilder(
            IFallbackStrategy fallbackStrategy,
            [Tag(Tags.SimpleBuildStrategy)] IBuildStrategy buildStrategy,
            [Tag(Tags.TypeStatementsStrategy)] IBindingStatementsStrategy bindingStatementsStrategy)
        {
            _fallbackStrategy = fallbackStrategy;
            _buildStrategy = buildStrategy;
            _bindingStatementsStrategy = bindingStatementsStrategy;
        }

        public ResolveMethod Build(SemanticModel semanticModel)
        {
            var returnDefault = _fallbackStrategy.Build(semanticModel, null, SyntaxFactory.ParseTypeName("type"), SyntaxFactory.DefaultExpression(SyntaxRepo.ObjectTypeSyntax));
            return new ResolveMethod(SyntaxRepo.StaticResolveMethodSyntax, true, _buildStrategy, _bindingStatementsStrategy, SyntaxFactory.ParseName("type"), returnDefault);
        }
    }
}