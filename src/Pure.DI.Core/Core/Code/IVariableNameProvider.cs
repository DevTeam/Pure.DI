namespace Pure.DI.Core.Code;

internal interface IVariableNameProvider
{
    string GetVariableName(DependencyNode node, int transientId);

    string GetLocalUniqueVariableName(string baseName);
}