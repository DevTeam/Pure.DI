namespace Pure.DI.Core;

internal sealed class BuildException : Exception
{
    public BuildException(Dependency dependency, string id, string message, params Location[] location)
        : base(message)
    {
        Dependency = dependency;
        Id = id;
        Locations = location;
    }

    public Dependency Dependency { get; }
    
    public string Id { get; }

    public Location[] Locations { get; }
}