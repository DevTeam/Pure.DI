namespace Pure.DI.Core.Code;

enum CodeBuilderKind
{
    Implementation,
    Factory,
    Construct,
    Enumerable,
    AsyncEnumerable,
    Array,
    Span,
    SpanConversion,
    Composition,
    CannotResolve,
    ExplicitDefaultValue
}
