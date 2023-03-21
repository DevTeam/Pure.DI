namespace Pure.DI.Core;

internal class CodeArgFieldsBuilder: IBuilder<ComposerInfo, ComposerInfo>
{
    public ComposerInfo Build(ComposerInfo composer, CancellationToken cancellationToken)
    {
        if (!composer.Args.Any())
        {
            return composer;
        }
        
        var code = composer.Code;
        var membersCounter = composer.MembersCount;
        foreach (var arg in composer.Args)
        {
            cancellationToken.ThrowIfCancellationRequested();
            code.AppendLine($"private {arg.Node.Type} {arg.Name};");
            membersCounter++;
        }

        return composer with { MembersCount = membersCounter };
    }
}