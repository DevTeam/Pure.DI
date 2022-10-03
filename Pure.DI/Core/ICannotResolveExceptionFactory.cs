namespace Pure.DI.Core;

internal interface ICannotResolveExceptionFactory
{
    HandledException Create(IBindingMetadata binding, IEnumerable<CodeError> errors);
}