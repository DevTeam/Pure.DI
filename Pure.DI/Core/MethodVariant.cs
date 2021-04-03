namespace Pure.DI.Core
{
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal class MethodVariant
    {
        public MethodDeclarationSyntax TargetMethod;
        public readonly bool HasDefaultTag;
        public readonly IBindingStatementsStrategy BindingStatementsStrategy;
        public readonly ExpressionSyntax TypeExpression;
        public readonly StatementSyntax[] PostStatements;

        public MethodVariant(
            MethodDeclarationSyntax targetMethod,
            bool hasDefaultTag,
            IBindingStatementsStrategy bindingStatementsStrategy,
            ExpressionSyntax typeExpression,
            params StatementSyntax[] postStatements)
        {
            TargetMethod = targetMethod;
            HasDefaultTag = hasDefaultTag;
            BindingStatementsStrategy = bindingStatementsStrategy;
            TypeExpression = typeExpression;
            PostStatements = postStatements;
        }
    }
}