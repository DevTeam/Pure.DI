namespace Pure.DI.Core;

internal readonly record struct CodeError(string Description, params Location[] Locations)
{
    public string Description { get; } = Description;
    
    public Location[] Locations { get; } = Locations;
}