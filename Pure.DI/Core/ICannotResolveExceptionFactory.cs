namespace Pure.DI.Core;

internal interface ICannotResolveExceptionFactory
{
    HandledException Create(IBindingMetadata binding, ExpressionSyntax? tag, string description, Location? location = default);
}