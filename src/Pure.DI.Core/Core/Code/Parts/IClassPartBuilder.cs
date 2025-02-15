namespace Pure.DI.Core.Code.Parts;

internal interface IClassPartBuilder: IBuilder<CompositionCode, CompositionCode>
{
    ClassPart Part { get; }
}