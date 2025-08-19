namespace Pure.DI.Core.Code;

interface INameProvider
{
    string GetVariableName(IDependencyNode node, int transientId);

    string GetOverrideVariableName(MdOverride @override);

    string GetLocalUniqueVariableName(string baseName);
}