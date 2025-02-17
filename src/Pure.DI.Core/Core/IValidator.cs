namespace Pure.DI.Core;

interface IValidator<in T>
{
    bool Validate(T data);
}