namespace Pure.DI.Core;

internal interface IValidator<in T>
{
    bool Validate(T data);
}