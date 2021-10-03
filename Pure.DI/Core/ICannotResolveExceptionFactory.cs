namespace Pure.DI.Core
{
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal interface ICannotResolveExceptionFactory
    {
        HandledException Create(BindingMetadata binding, ExpressionSyntax? tag, string description);
    }
}