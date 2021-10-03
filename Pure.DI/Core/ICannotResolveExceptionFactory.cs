namespace Pure.DI.Core
{
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal interface ICannotResolveExceptionFactory
    {
        HandledException Create(IBindingMetadata binding, ExpressionSyntax? tag, string description);
    }
}