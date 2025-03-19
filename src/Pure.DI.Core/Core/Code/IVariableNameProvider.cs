namespace Pure.DI.Core.Code;

interface IVariableNameProvider
{
    string GetVariableName(DependencyNode node, int transientId);

    string GetOverrideVariableName(MdOverride @override);

    string GetLocalUniqueVariableName(string baseName);
}