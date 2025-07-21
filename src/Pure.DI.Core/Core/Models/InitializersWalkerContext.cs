namespace Pure.DI.Core.Models;

record InitializersWalkerContext(
    Action<VarInjection> BuildVarInjection,
    string VariableName,
    IEnumerator<VarInjection> VarInjections);