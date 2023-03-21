namespace Pure.DI.Core;

internal class CodeDisposeMethodBuilder: IBuilder<ComposerInfo, ComposerInfo>
{
    public ComposerInfo Build(ComposerInfo composer, CancellationToken cancellationToken)
    {
        var code = composer.Code;
        var membersCounter = composer.MembersCount;
        if (composer.Singletons.Any())
        {
            if (composer.MembersCount > 0)
            {
                code.AppendLine();
            }

            code.AppendLine("public void Dispose()");
            code.AppendLine("{");
            using (code.Indent())
            {
                code.AppendLine($"lock ({Variable.DisposablesFieldName})");
                code.AppendLine("{");
                using (code.Indent())
                {
                    if (composer.DisposableSingletonsCount > 0)
                    {
                        code.AppendLine($"while ({Variable.DisposeIndexFieldName}-- >= 0)");
                        code.AppendLine("{");
                        using (code.Indent())
                        {
                            code.AppendLine("try");
                            code.AppendLine("{");
                            using (code.Indent())
                            {
                                code.AppendLine($"{Variable.DisposablesFieldName}[{Variable.DisposeIndexFieldName}].Dispose();");
                            }

                            code.AppendLine("}");
                            code.AppendLine("catch");
                            code.AppendLine("{");
                            using (code.Indent())
                            {
                                code.AppendLine("// ignored");
                            }

                            code.AppendLine("}");
                        }

                        code.AppendLine("}");
                        code.AppendLine();
                    }

                    foreach (var singletonField in composer.Singletons)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        code.AppendLine(
                            singletonField.Node.Type.IsValueType
                                ? $"{singletonField.Name}Created = false;"
                                : $"{singletonField.Name} = null;");
                    }
                }

                code.AppendLine("}");
            }

            code.AppendLine("}");
        }

        return composer with { MembersCount = membersCounter };
    }
}