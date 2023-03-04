namespace Pure.DI.Core.Models;

internal readonly record struct FactoryInjection(
    string InjectionId,
    int ContextId,
    bool NeedsDeclaration,
    string VariableName);