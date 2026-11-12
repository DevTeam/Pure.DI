namespace Pure.DI.Core.Code;

interface INameProvider
{
    IDisposable Root();

    string GetVariableName(IDependencyNode node);

    string GetOverrideVariableName(MdOverride @override);

    string GetLocalUniqueVariableName(string baseName);

    void ReserveRootName(string name);

    string GetUniqueRootName(string name, ITypeSymbol rootType);
}
