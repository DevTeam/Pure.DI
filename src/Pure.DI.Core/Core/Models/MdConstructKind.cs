// ReSharper disable InconsistentNaming

namespace Pure.DI.Core.Models;

enum MdConstructKind
{
    None,
    Enumerable,
    Array,
    Span,
    ImplicitConversion,
    Composition,
    OnCannotResolve,
    ExplicitDefaultValue,
    AsyncEnumerable,
    Accumulator,
    Override
}
