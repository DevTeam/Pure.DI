namespace Pure.DI.InterfaceGeneration;

interface IInterfaceAttributeSources
{
    string GenerateInterfaceAttributeSource { get; }

    string IgnoreInterfaceAttributeSource { get; }
}