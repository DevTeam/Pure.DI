// ReSharper disable HeapView.BoxingAllocation
// ReSharper disable ClassNeverInstantiated.Global

namespace Pure.DI.Core.Code;

sealed class UsingDeclarationsBuilder : IBuilder<CompositionCode, CompositionCode>
{
    public CompositionCode Build(CompositionCode composition)
    {
        var code = composition.Code;
        if (composition.Source.Source.UsingDirectives.Length == 0)
        {
            return composition;
        }

        foreach (var usingDirective in composition.Source.Source.UsingDirectives.SelectMany(i => i.UsingDirectives).Distinct())
        {
            code.AppendLine($"using {usingDirective};");
        }

        foreach (var staticUsingDirective in composition.Source.Source.UsingDirectives.SelectMany(i => i.StaticUsingDirectives).Distinct())
        {
            code.AppendLine($"using static {staticUsingDirective};");
        }

        foreach (var alias in composition.Source.Source.UsingDirectives.SelectMany(i => i.Aliases))
        {
            code.AppendLine($"using {alias.name}={alias.type};");
        }

        code.AppendLine();
        return composition;
    }
}