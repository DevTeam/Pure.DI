namespace Pure.DI.Core.Code;

interface IVariableNameProvider
{
    string GetVariableName(IDependencyNode node, int transientId);

    string GetOverrideVariableName(MdOverride @override);

    string GetLocalUniqueVariableName(string baseName);
}