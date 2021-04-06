namespace Pure.DI.Core
{
    using System.Linq;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    // ReSharper disable once ClassNeverInstantiated.Global
    internal class BindingsProbe : IBindingsProbe
    {
        private readonly IBuildContext _buildContext;
        private readonly IBindingExpressionStrategy _bindingExpressionStrategy;

        public BindingsProbe(
            IBuildContext buildContext,
            [Tag(Tags.SimpleExpressionStrategy)] IBindingExpressionStrategy bindingExpressionStrategy)
        {
            _buildContext = buildContext;
            _bindingExpressionStrategy = bindingExpressionStrategy;
        }

        public void Probe()
        {
            // Find additional bindings
            (
                from binding in _buildContext.Metadata.Bindings
                from contractType in binding.ContractTypes
                where contractType.IsValidTypeToResolve(_buildContext.SemanticModel)
                // ReSharper disable once RedundantTypeArgumentsOfMethod
                from tag in binding.Tags.DefaultIfEmpty<ExpressionSyntax?>(null)
                select _bindingExpressionStrategy.TryBuild(contractType, tag))
                // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
                .ToList();
        }
    }
}
