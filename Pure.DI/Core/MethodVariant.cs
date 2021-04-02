namespace Pure.DI.Core
{
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal class MethodVariant
    {
        public MethodDeclarationSyntax TargetMethod;
        public readonly bool HasDefaultTag;
        public readonly IBindingStatementsStrategy BindingStatementsStrategy;
        public readonly ExpressionSyntax TypeExpression;
        public readonly ReturnStatementSyntax DefaultReturnStatement;

        public MethodVariant(
            MethodDeclarationSyntax targetMethod,
            bool hasDefaultTag,
            IBindingStatementsStrategy bindingStatementsStrategy,
            ExpressionSyntax typeExpression,
            ReturnStatementSyntax defaultReturnStatement)
        {
            TargetMethod = targetMethod;
            HasDefaultTag = hasDefaultTag;
            BindingStatementsStrategy = bindingStatementsStrategy;
            TypeExpression = typeExpression;
            DefaultReturnStatement = defaultReturnStatement;
        }
    }
}