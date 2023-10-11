// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core.Code;

internal sealed class DisposeMethodBuilder: IBuilder<CompositionCode, CompositionCode>
{
    public CompositionCode Build(CompositionCode composition)
    {
        var code = composition.Code;
        var membersCounter = composition.MembersCount;
        if (composition.DisposableSingletonsCount == 0)
        {
            return composition with { MembersCount = membersCounter };
        }
        
        if (composition.MembersCount > 0)
        {
            code.AppendLine();
        }
        
        code.AppendLine($"{composition.Source.Source.Hints.GetValueOrDefault(Hint.DisposeMethodModifiers, Names.DefaultApiMethodModifiers)} void Dispose()");
        code.AppendLine("{");
        using (code.Indent())
        {
            code.AppendLine($"lock ({Names.DisposablesFieldName})");
            code.AppendLine("{");
            using (code.Indent())
            {
                code.AppendLine($"while ({Names.DisposeIndexFieldName} > 0)");
                code.AppendLine("{");
                using (code.Indent())
                {
                    code.AppendLine($"var disposableInstance = {Names.DisposablesFieldName}[--{Names.DisposeIndexFieldName}];");
                    code.AppendLine("try");
                    code.AppendLine("{");
                    using (code.Indent())
                    {
                        code.AppendLine($"disposableInstance.Dispose();");
                    }

                    code.AppendLine("}");
                    code.AppendLine("catch(Exception exception)");
                    code.AppendLine("{");
                    using (code.Indent())
                    {
                        code.AppendLine($"{Names.OnDisposeExceptionMethodName}(disposableInstance, exception);");
                    }

                    code.AppendLine("}");
                }

                code.AppendLine("}");
                code.AppendLine();

                foreach (var singletonField in composition.Singletons)
                {
                    code.AppendLine(
                        singletonField.InstanceType.IsValueType
                            ? $"{singletonField.VariableName}Created = false;"
                            : $"{singletonField.VariableName} = null;");
                }
            }

            code.AppendLine("}");
        }

        code.AppendLine("}");
        membersCounter++;
        
        code.AppendLine();
        code.AppendLine($"partial void {Names.OnDisposeExceptionMethodName}<T>(T disposableInstance, Exception exception) where T : {Names.IDisposableInterfaceName};");
        membersCounter++;

        return composition with { MembersCount = membersCounter };
    }
}