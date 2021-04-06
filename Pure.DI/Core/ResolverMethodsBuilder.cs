namespace Pure.DI.Core
{
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    // ReSharper disable once ClassNeverInstantiated.Global
    [SuppressMessage("ReSharper", "RedundantTypeArgumentsOfMethod")]
    internal class ResolverMethodsBuilder : IResolverMethodsBuilder
    {
        private readonly ResolverMetadata _metadata;
        private readonly SemanticModel _semanticModel;
        private readonly IBindingsProbe _bindingsProbe;
        private readonly IResolveMethodBuilder[] _resolveMethodBuilders;
        private readonly IBuildContext _buildContext;

        public ResolverMethodsBuilder(
            ResolverMetadata metadata,
            SemanticModel semanticModel,
            IBindingsProbe bindingsProbe,
            IResolveMethodBuilder[] resolveMethodBuilders,
            IBuildContext buildContext)
        {
            _metadata = metadata;
            _semanticModel = semanticModel;
            _bindingsProbe = bindingsProbe;
            _resolveMethodBuilders = resolveMethodBuilders;
            _buildContext = buildContext;
        }

        public IEnumerable<MemberDeclarationSyntax> CreateResolveMethods()
        {
            _buildContext.Prepare();
            _bindingsProbe.Probe();

            var allMethods = _resolveMethodBuilders.Select(i => i.Build()).ToArray();

            var resolvedMethods =
                from binding in _metadata.Bindings.Reverse().Concat(_buildContext.AdditionalBindings).Distinct().ToList()
                from contractType in binding.ContractTypes
                where contractType.IsValidTypeToResolve(_semanticModel)
                from tag in binding.Tags.DefaultIfEmpty<ExpressionSyntax?>(null)
                from variant in allMethods
                where variant.HasDefaultTag == (tag == null)
                let statement = ResolveStatement(_semanticModel, contractType, variant, binding)
                group (variant, statement) by (variant.TargetMethod.ToString(), statement.ToString(), contractType.ToString(), tag?.ToString()) into groupedByStatement
                // Avoid duplication of statements
                select groupedByStatement.First();

            // Body
            foreach (var (method, statement) in resolvedMethods)
            {
                method.TargetMethod = method.TargetMethod
                    .AddBodyStatements(statement);
            }

            // Post statements
            foreach (var method in allMethods)
            {
                method.TargetMethod = method.TargetMethod
                    .AddBodyStatements(method.PostStatements);
            }

            return allMethods
                .Select(strategy => strategy.TargetMethod)
                .Concat(_buildContext.AdditionalMembers);
        }

        private static IfStatementSyntax ResolveStatement(
            SemanticModel semanticModel,
            ITypeSymbol contractType,
            ResolveMethod resolveMethod,
            BindingMetadata binding) =>
            SyntaxFactory.IfStatement(
                SyntaxFactory.BinaryExpression(
                    SyntaxKind.EqualsExpression,
                    SyntaxFactory.TypeOfExpression(contractType.ToTypeSyntax(semanticModel)),
                    resolveMethod.TypeExpression),
                SyntaxFactory.Block(resolveMethod.BindingStatementsStrategy.CreateStatements(resolveMethod.BindingExpressionStrategy, binding, contractType))
            );
    }
}
