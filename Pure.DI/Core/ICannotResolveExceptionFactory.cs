namespace Pure.DI.Core;

internal interface ICannotResolveExceptionFactory
{
    HandledException Create(IBindingMetadata binding, ExpressionSyntax? tag, string description, params Location[] locations);
}