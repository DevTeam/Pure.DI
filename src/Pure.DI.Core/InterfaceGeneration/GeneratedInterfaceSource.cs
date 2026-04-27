namespace Pure.DI.InterfaceGeneration;

sealed class GeneratedInterfaceSource(
    string namespaceName,
    string interfaceName,
    Lines code)
{
    public string NamespaceName { get; } = namespaceName;

    public string InterfaceName { get; } = interfaceName;

    public Lines Code { get; } = code;
}
