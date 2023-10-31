namespace Pure.DI.Core;

internal interface IValidator<T>
{
    bool Validate(in T data);
}