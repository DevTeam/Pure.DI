namespace Pure.DI.Core.Models;

public readonly record struct CompositionName(string ClassName, string Namespace)
{
    public string FullName => 
        string.IsNullOrWhiteSpace(Namespace) ? ClassName : Namespace + "." + ClassName;

    public override string ToString() => FullName;
}