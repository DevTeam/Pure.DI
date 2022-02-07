namespace Pure.DI.Core;

internal readonly record struct Optional<T>(T Value, bool HasValue = true, string Description = "", Location? Location = default)
    where T: class
{
    public T Value { get; } = Value;

    public bool HasValue { get; } = HasValue;

    public string Description { get; } = Description;

    public Location? Location { get; } = Location;

    public static implicit operator Optional<T>(T value) => new(value);
    
    public override string ToString() => HasValue ? Value?.ToString() ?? $"null: {Description}" : "unspecified";

    public static Optional<T> CreateEmpty(string description, Location? location) => 
        new(default!, false, description, location);
}