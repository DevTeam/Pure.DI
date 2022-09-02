namespace Pure.DI.Core;

internal readonly record struct CodeError(Dependency Dependency, string Id, string Description, params Location[] Locations)
{
    public Dependency Dependency { get; } = Dependency;

    public string Id { get; } = Id;

    public string Description { get; } = Description;
    
    public Location[] Locations { get; } = Locations;
}