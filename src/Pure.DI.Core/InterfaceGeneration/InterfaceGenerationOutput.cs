namespace Pure.DI.InterfaceGeneration;

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

/// <summary>
/// The interfaces generated for one class. It is compared by value, so that Roslyn adds the sources again
/// only when the generated code or the warnings change.
/// </summary>
public sealed class InterfaceGenerationOutput(
    ImmutableArray<InterfaceGenerationSource> sources,
    ImmutableArray<InterfaceGenerationWarning> warnings)
    : IEquatable<InterfaceGenerationOutput>
{
    public static readonly InterfaceGenerationOutput Empty = new(
        ImmutableArray<InterfaceGenerationSource>.Empty,
        ImmutableArray<InterfaceGenerationWarning>.Empty);

    public ImmutableArray<InterfaceGenerationSource> Sources { get; } = sources;

    public ImmutableArray<InterfaceGenerationWarning> Warnings { get; } = warnings;

    public bool Equals(InterfaceGenerationOutput? other) =>
        other is not null
        && Sources.SequenceEqual(other.Sources)
        && Warnings.SequenceEqual(other.Warnings);

    public override bool Equals(object? obj) => Equals(obj as InterfaceGenerationOutput);

    public override int GetHashCode()
    {
        var hash = 17;
        foreach (var source in Sources)
        {
            hash = hash * 31 + source.GetHashCode();
        }

        return hash * 31 + Warnings.Length;
    }
}

public readonly record struct InterfaceGenerationSource(string HintName, string Code);

public sealed class InterfaceGenerationWarning(
    string id,
    string message,
    string messageKey,
    Location location)
    : IEquatable<InterfaceGenerationWarning>
{
    public string Id { get; } = id;

    public string Message { get; } = message;

    public string MessageKey { get; } = messageKey;

    public Location Location { get; } = location;

    // A location refers to the syntax tree it was found in, and the tree is replaced after each edit of its file,
    // so locations are compared by the file and the position in it.
    public bool Equals(InterfaceGenerationWarning? other) =>
        other is not null
        && Id == other.Id
        && Message == other.Message
        && MessageKey == other.MessageKey
        && Location.Kind == other.Location.Kind
        && Location.SourceSpan == other.Location.SourceSpan
        && Location.GetLineSpan().Equals(other.Location.GetLineSpan());

    public override bool Equals(object? obj) => Equals(obj as InterfaceGenerationWarning);

    public override int GetHashCode() => Id.GetHashCode() * 31 + Location.SourceSpan.GetHashCode();
}
