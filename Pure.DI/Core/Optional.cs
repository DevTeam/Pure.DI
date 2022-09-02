namespace Pure.DI.Core;

internal readonly record struct Optional<T>(T Value, bool HasValue = true, params CodeError[] Errors)
    where T: class
{
    private readonly CodeError[] _errors = Errors;
    
    public T Value { get; } = Value;

    public bool HasValue { get; } = HasValue;

    public CodeError[] Errors => _errors ?? Array.Empty<CodeError>();

    public static implicit operator Optional<T>(T value) => new(value);
    
    public static Optional<T> CreateEmpty(params CodeError[] errors) => 
        new(default!, false, errors);
}