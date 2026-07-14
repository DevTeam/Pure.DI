// ReSharper disable InconsistentNaming

namespace Pure.DI.Core.Models;

enum MdConstructKind
{
    None,
    Enumerable,
    Array,
    Span,
    SpanConversion,
    Composition,
    OnCannotResolve,
    ExplicitDefaultValue,
    AsyncEnumerable,
    Accumulator,
    Override
}
