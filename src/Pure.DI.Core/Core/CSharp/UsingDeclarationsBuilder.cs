namespace Pure.DI.Core.CSharp;

internal class UsingDeclarationsBuilder: IBuilder<CompositionCode, CompositionCode>
{
    public CompositionCode Build(CompositionCode composition, CancellationToken cancellationToken)
    {
        var code = composition.Code;
        if (!composition.UsingDirectives.Any())
        {
            return composition;
        }

        foreach (var usingDirective in composition.UsingDirectives)
        {
            code.AppendLine($"using {usingDirective};");
        }

        code.AppendLine();
        return composition;
    }
}