namespace Pure.DI.Core
{
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal class ResolveMethod
    {
        public MethodDeclarationSyntax TargetMethod;
        public readonly bool HasDefaultTag;
        public readonly IBindingExpressionStrategy BindingExpressionStrategy;
        public readonly IBindingStatementsStrategy BindingStatementsStrategy;
        public readonly ExpressionSyntax TypeExpression;
        public readonly StatementSyntax[] PostStatements;

        public ResolveMethod(
            MethodDeclarationSyntax targetMethod,
            bool hasDefaultTag,
            IBindingExpressionStrategy bindingExpressionStrategy,
            IBindingStatementsStrategy bindingStatementsStrategy,
            ExpressionSyntax typeExpression,
            params StatementSyntax[] postStatements)
        {
            TargetMethod = targetMethod;
            HasDefaultTag = hasDefaultTag;
            BindingExpressionStrategy = bindingExpressionStrategy;
            BindingStatementsStrategy = bindingStatementsStrategy;
            TypeExpression = typeExpression;
            PostStatements = postStatements;
        }
    }
}