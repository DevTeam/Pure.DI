namespace Pure.DI.Core.CSharp;

internal class CodeDefaultConstructorBuilder: IBuilder<ComposerCode, ComposerCode>
{
    public ComposerCode Build(ComposerCode composer, CancellationToken cancellationToken)
    {
        if (composer.Args.Any())
        {
            return composer;
        }

        var code = composer.Code;
        var membersCounter = composer.MembersCount;
        if (membersCounter > 0)
        {
            code.AppendLine();
        }

        code.AppendLine($"public {composer.ClassName}()");
        code.AppendLine("{");
        if (composer.Singletons.Any())
        {
            using (code.Indent())
            {
                code.AppendLine($"{Variable.DisposablesFieldName} = new {CodeConstants.DisposableTypeName}[{composer.DisposableSingletonsCount}];");
            }
        }

        code.AppendLine("}");
        membersCounter++;
        return composer with { MembersCount = membersCounter };
    }
}