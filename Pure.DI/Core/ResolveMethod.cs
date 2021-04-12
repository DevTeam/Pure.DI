namespace Pure.DI.Core
{
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal class ResolveMethod
    {
        public MethodDeclarationSyntax TargetMethod;
        public readonly bool HasDefaultTag;
        public readonly IBuildStrategy BuildStrategy;
        public readonly IBindingStatementsStrategy BindingStatementsStrategy;
        public readonly ExpressionSyntax TypeExpression;
        public readonly StatementSyntax[] PostStatements;

        public ResolveMethod(
            MethodDeclarationSyntax targetMethod,
            bool hasDefaultTag,
            IBuildStrategy buildStrategy,
            IBindingStatementsStrategy bindingStatementsStrategy,
            ExpressionSyntax typeExpression,
            params StatementSyntax[] postStatements)
        {
            TargetMethod = targetMethod;
            HasDefaultTag = hasDefaultTag;
            BuildStrategy = buildStrategy;
            BindingStatementsStrategy = bindingStatementsStrategy;
            TypeExpression = typeExpression;
            PostStatements = postStatements;
        }
    }
}