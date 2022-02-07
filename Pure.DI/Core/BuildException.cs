namespace Pure.DI.Core;

public class BuildException : Exception
{
    public BuildException(string id, string message, params Location[] location)
        : base(message)
    {
        Id = id;
        Locations = location;
    }

    public string Id { get; }

    public Location[] Locations { get; }
}