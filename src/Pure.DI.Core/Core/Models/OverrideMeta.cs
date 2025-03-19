namespace Pure.DI.Core.Models;

readonly record struct OverrideMeta(
    int Position,
    InvocationExpressionSyntax Expression);