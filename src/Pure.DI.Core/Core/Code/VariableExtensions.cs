namespace Pure.DI.Core.Code;

internal static class VariableExtensions
{
    public static bool IsDelegate(this Variable variable) =>
        variable.ContractType is INamedTypeSymbol { DelegateInvokeMethod: not null };
}