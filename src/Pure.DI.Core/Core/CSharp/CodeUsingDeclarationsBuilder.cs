namespace Pure.DI.Core.CSharp;

internal class CodeUsingDeclarationsBuilder: IBuilder<ComposerCode, ComposerCode>
{
    public ComposerCode Build(ComposerCode composer, CancellationToken cancellationToken)
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