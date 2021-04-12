namespace Pure.DI.Core
{
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Runtime.InteropServices;
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
                from method in allMethods
                where method.HasDefaultTag == (tag == null)
                let statement = ResolveStatement(_semanticModel, contractType, method, binding)
                group (method, statement) by (method.TargetMethod.ToString(), statement.ToString(), contractType.ToString(), tag?.ToString()) into groupedByStatement
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

            List<MemberDeclarationSyntax> internalMembers = new();
            if (_buildContext.FinalizationStatements.Any())
            {
                const string deepnessFieldName = "_deepness";
                var deepnessField = SyntaxFactory.FieldDeclaration(
                        SyntaxFactory.VariableDeclaration(_semanticModel.Compilation.GetSpecialType(SpecialType.System_Int32).ToTypeSyntax(_semanticModel))
                            .AddVariables(
                                SyntaxFactory.VariableDeclarator(deepnessFieldName)
                            )
                    )
                    .AddModifiers(
                        SyntaxFactory.Token(SyntaxKind.PrivateKeyword),
                        SyntaxFactory.Token(SyntaxKind.StaticKeyword));

                internalMembers.Add(deepnessField);

                var refToDeepness = SyntaxFactory.Argument(SyntaxFactory.IdentifierName(deepnessFieldName)).WithRefKindKeyword(SyntaxFactory.Token(SyntaxKind.RefKeyword));

                var incrementStatement = SyntaxFactory.InvocationExpression(
                    SyntaxFactory.IdentifierName("System.Threading.Interlocked.Increment"),
                    SyntaxFactory.ArgumentList().AddArguments(refToDeepness));

                var decrementStatement = SyntaxFactory.InvocationExpression(
                    SyntaxFactory.IdentifierName("System.Threading.Interlocked.Decrement"),
                    SyntaxFactory.ArgumentList().AddArguments(refToDeepness));

                foreach (var method in allMethods)
                {
                    var curStatements = method.TargetMethod.Body;
                    if (curStatements != null)
                    {
                        var releaseBlock = SyntaxFactory.Block()
                            .AddStatements(SyntaxFactory.ExpressionStatement(decrementStatement))
                            .AddStatements(
                                SyntaxFactory.IfStatement(
                                    SyntaxFactory.BinaryExpression(SyntaxKind.EqualsExpression, SyntaxFactory.IdentifierName(deepnessFieldName), SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(0))),
                                        SyntaxFactory.Block().AddStatements(_buildContext.FinalizationStatements.ToArray())
                                ));
                        
                        var tryStatement = SyntaxFactory.TryStatement(
                            curStatements,
                            SyntaxFactory.List<CatchClauseSyntax>(),
                            SyntaxFactory.FinallyClause(releaseBlock));

                        method.TargetMethod = method.TargetMethod.WithBody(
                            SyntaxFactory.Block()
                                .AddStatements(SyntaxFactory.ExpressionStatement(incrementStatement))
                                .AddStatements(tryStatement));
                    }
                }
            }

            return allMethods
                .Select(strategy => strategy.TargetMethod)
                .Concat(_buildContext.AdditionalMembers)
                .Concat(internalMembers);
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
                SyntaxFactory.Block(resolveMethod.BindingStatementsStrategy.CreateStatements(resolveMethod.BuildStrategy, binding, contractType))
            );
    }
}
