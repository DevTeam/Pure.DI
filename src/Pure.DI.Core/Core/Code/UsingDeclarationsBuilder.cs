// ReSharper disable HeapView.BoxingAllocation
// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core.Code;

internal sealed class UsingDeclarationsBuilder: IBuilder<CompositionCode, CompositionCode>
{
    public CompositionCode Build(CompositionCode composition)
    {
        var code = composition.Code;
        if (!composition.UsingDirectives.Any())
        {
            return composition;
        }
        
        foreach (var usingDirective in composition.UsingDirectives.SelectMany(i => i.UsingDirectives).OrderBy(i => i).Distinct())
        {
            code.AppendLine($"using {usingDirective};");
        }
        
        foreach (var usingDirective in composition.UsingDirectives.SelectMany(i => i.StaticUsingDirectives).OrderBy(i => i).Distinct())
        {
            code.AppendLine($"using static {usingDirective};");
        }
        
        code.AppendLine();
        return composition;
    }
}