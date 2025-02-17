namespace Pure.DI.Core.Code;

interface IVariableNameProvider
{
    string GetVariableName(DependencyNode node, int transientId);

    string GetLocalUniqueVariableName(string baseName);
}