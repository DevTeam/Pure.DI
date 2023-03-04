namespace Pure.DI.Core;

internal interface IValidator<T>
{
    void Validate(in T data, in CancellationToken cancellationToken);
}