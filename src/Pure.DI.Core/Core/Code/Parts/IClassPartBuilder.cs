namespace Pure.DI.Core.Code.Parts;

interface IClassPartBuilder : IBuilder<CompositionCode, CompositionCode>
{
    ClassPart Part { get; }
}