namespace Pure.DI.Core;

internal class CodeChildConstructorBuilder: IBuilder<ComposerInfo, ComposerInfo>
{
    private const string ParentComposerArgName = "parent";

    public ComposerInfo Build(ComposerInfo composer, CancellationToken cancellationToken)
    {
        var code = composer.Code;
        var membersCounter = composer.MembersCount;
        if (membersCounter > 0)
        {
            code.AppendLine();
        }

        code.AppendLine($"internal {composer.ClassName}({composer.ClassName} {ParentComposerArgName})");
        code.AppendLine("{");
        if (composer.Singletons.Any())
        {
            using (code.Indent())
            {
                code.AppendLine($"lock ({ParentComposerArgName}.{Variable.DisposablesFieldName})");
                code.AppendLine("{");
                using (code.Indent())
                {
                    code.AppendLine($"{Variable.DisposablesFieldName} = new {CodeConstants.DisposableTypeName}[{composer.DisposableSingletonsCount} - {ParentComposerArgName}.{Variable.DisposablesFieldName}.Length];");
                    foreach (var singletonField in composer.Singletons)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        code.AppendLine($"{singletonField.Name} = {ParentComposerArgName}.{singletonField.Name};");

                        if (singletonField.Node.Type.IsValueType)
                        {
                            code.AppendLine($"{singletonField.Name}Created = {ParentComposerArgName}.{singletonField.Name}Created;");
                        }
                    }
                }

                code.AppendLine("}");
            }
        }

        if (composer.Args.Any())
        {
            using (code.Indent())
            {
                foreach (var argsField in composer.Args)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    code.AppendLine($"{argsField.Name} = {ParentComposerArgName}.{argsField.Name};");
                }
            }
        }

        code.AppendLine("}");
        membersCounter++;
        return composer with { MembersCount = membersCounter };
    }
}