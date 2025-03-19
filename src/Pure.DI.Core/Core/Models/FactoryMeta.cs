namespace Pure.DI.Core.Models;

readonly record struct FactoryMeta(
    FactoryMetaKind Kind,
    InvocationExpressionSyntax Expression,
    IReadOnlyCollection<OverrideMeta> Overrides);