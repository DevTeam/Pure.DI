// Minimal runtime support for the preview C# union types feature.
// These types are required by the compiler when a `union` declaration is emitted
// and are not yet available in the current target framework.
// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedType.Global

// ReSharper disable once CheckNamespace
namespace System.Runtime.CompilerServices;

public interface IUnion
{
    object? Value { get; }
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public sealed class UnionAttribute : Attribute;
