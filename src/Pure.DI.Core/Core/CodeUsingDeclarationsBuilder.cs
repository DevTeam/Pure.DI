namespace Pure.DI.Core;

internal class CodeUsingDeclarationsBuilder: IBuilder<ComposerInfo, ComposerInfo>
{
    public ComposerInfo Build(ComposerInfo composer, CancellationToken cancellationToken)
    {
        var code = composer.Code;
        if (!composer.UsingDirectives.Any())
        {
            return composer;
        }

        foreach (var usingDirective in composer.UsingDirectives)
        {
            code.AppendLine($"using {usingDirective};");
        }

        code.AppendLine();
        return composer;
    }
}