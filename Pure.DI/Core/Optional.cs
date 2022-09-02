namespace Pure.DI.Core;

internal readonly record struct Optional<T>(T Value, bool HasValue = true, params CodeError[] Errors)
    where T: class
{
    public T Value { get; } = Value;

    public bool HasValue { get; } = HasValue;
    
    public CodeError[] Errors { get; } = Errors;

    public static implicit operator Optional<T>(T value) => new(value);
    
    public static Optional<T> CreateEmpty(params CodeError[] Errors) => 
        new(default!, false, Errors);
}