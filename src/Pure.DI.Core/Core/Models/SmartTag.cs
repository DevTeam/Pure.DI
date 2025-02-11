namespace Pure.DI.Core.Models;

internal sealed class SmartTag(string name)
{
    public string Name { get; } = name;

    public override string ToString() => $"SmartTag(\"{Name}\")";

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        var other = (SmartTag)obj;
        return Name == other.Name;
    }

    public override int GetHashCode() => 
        Name.GetHashCode();
}