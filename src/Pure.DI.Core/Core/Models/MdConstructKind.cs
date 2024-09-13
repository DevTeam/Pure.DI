// ReSharper disable InconsistentNaming

namespace Pure.DI.Core.Models;

internal enum MdConstructKind
{
    None,
    Enumerable,
    Array,
    Span,
    Composition,
    OnCannotResolve,
    ExplicitDefaultValue,
    AsyncEnumerable,
    Accumulator
}