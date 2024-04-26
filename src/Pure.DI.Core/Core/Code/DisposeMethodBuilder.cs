// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core.Code;

internal sealed class DisposeMethodBuilder(
    IAsyncDisposableSettings asyncDisposableSettings)
    : IBuilder<CompositionCode, CompositionCode>
{
    public CompositionCode Build(CompositionCode composition)
    {
        var code = composition.Code;
        var membersCounter = composition.MembersCount;
        if (composition.TotalDisposablesCount == 0)
        {
            return composition with { MembersCount = membersCounter };
        }
        
        if (composition.MembersCount > 0)
        {
            code.AppendLine();
        }

        var hints = composition.Source.Source.Hints;
        var isCommentsEnabled = hints.IsCommentsEnabled;
        if (isCommentsEnabled)
        {
            code.AppendLine("/// <summary>");
            code.AppendLine("/// <inheritdoc/>");
            code.AppendLine("/// </summary>");
        }

        code.AppendLine($"{composition.Source.Source.Hints.DisposeMethodModifiers} void Dispose()");
        code.AppendLine("{");
        using (code.Indent())
        {
            AddSyncPart(composition, code);

            code.AppendLine("while (disposeIndex > 0)");
            code.AppendLine("{");
            using (code.Indent())
            {
                code.AppendLine("var instance = disposables[--disposeIndex];");
                AddDisposePart(code);
                AddAsyncDisposePart(composition, code, false);
            }

            code.AppendLine("}");
            code.AppendLine();
        }

        code.AppendLine("}");
        membersCounter++;

        if (composition.AsyncDisposableCount > 0 && asyncDisposableSettings.IsEnabled(composition.Source.Source.SemanticModel.Compilation))
        {
            if (isCommentsEnabled)
            {
                code.AppendLine("/// <summary>");
                code.AppendLine("/// <inheritdoc/>");
                code.AppendLine("/// </summary>");
            }

            code.AppendLine($"{composition.Source.Source.Hints.DisposeMethodModifiers} async {Names.ValueTaskName} DisposeAsync()");
            code.AppendLine("{");
            using (code.Indent())
            {
                AddSyncPart(composition, code);

                code.AppendLine("while (disposeIndex > 0)");
                code.AppendLine("{");
                using (code.Indent())
                {
                    code.AppendLine("var instance = disposables[--disposeIndex];");
                    AddAsyncDisposePart(composition, code, true);
                    AddDisposePart(code);
                }

                code.AppendLine("}");
                code.AppendLine();
            }

            code.AppendLine("}");
            membersCounter++;
        }
        
        code.AppendLine();
        code.AppendLine("/// <summary>");
        code.AppendLine("/// Implement this partial method to handle the exception on disposing.");
        code.AppendLine("/// <summary>");
        code.AppendLine("/// <param name=\"disposableInstance\">The disposable instance.</param>");
        code.AppendLine("/// <param name=\"exception\">Exception occurring during disposal.</param>");
        code.AppendLine("/// <typeparam name=\"T\">The actual type of instance being disposed of.</typeparam>");
        code.AppendLine($"partial void {Names.OnDisposeExceptionMethodName}<T>(T disposableInstance, Exception exception) where T : {Names.IDisposableInterfaceName};");
        code.AppendLine();
        membersCounter++;

        // ReSharper disable once InvertIf
        if (asyncDisposableSettings.IsEnabled(composition.Source.Source.SemanticModel.Compilation))
        {
            code.AppendLine();
            code.AppendLine("/// <summary>");
            code.AppendLine("/// Implement this partial method to handle the exception on async disposing.");
            code.AppendLine("/// <summary>");
            code.AppendLine("/// <param name=\"asyncDisposableInstance\">The disposable instance.</param>");
            code.AppendLine("/// <param name=\"exception\">Exception occurring during disposal.</param>");
            code.AppendLine("/// <typeparam name=\"T\">The actual type of instance being disposed of.</typeparam>");
            code.AppendLine($"partial void {Names.OnAsyncDisposeExceptionMethodName}<T>(T asyncDisposableInstance, Exception exception) where T : {Names.IAsyncDisposableInterfaceName};");
            code.AppendLine();
            membersCounter++;
        }

        return composition with { MembersCount = membersCounter };
    }

    private void AddAsyncDisposePart(CompositionCode composition, LinesBuilder code, bool makeAsyncCall)
    {
        if (!asyncDisposableSettings.IsEnabled(composition.Source.Source.SemanticModel.Compilation))
        {
            return;
        }
        
        code.AppendLine();
        code.AppendLine($"var asyncDisposableInstance = instance as {Names.IAsyncDisposableInterfaceName};");
        code.AppendLine("if (asyncDisposableInstance != null)");
        code.AppendLine("{");
        using (code.Indent())
        {
            code.AppendLine("try");
            code.AppendLine("{");
            using (code.Indent())
            {
                if (makeAsyncCall)
                {
                    code.AppendLine("await asyncDisposableInstance.DisposeAsync();");
                }
                else
                {
                    code.AppendLine("var valueTask = asyncDisposableInstance.DisposeAsync();");
                    code.AppendLine("if (!valueTask.IsCompleted)");
                    code.AppendLine("{");
                    using (code.Indent())
                    {
                        code.AppendLine("valueTask.AsTask().Wait();");
                    }
                    code.AppendLine("}");   
                }
            }

            code.AppendLine("}");
            code.AppendLine("catch (Exception exception)");
            code.AppendLine("{");
            using (code.Indent())
            {
                code.AppendLine($"{Names.OnAsyncDisposeExceptionMethodName}(asyncDisposableInstance, exception);");
            }
            
            code.AppendLine("}");
        }

        code.AppendLine("continue;");
        code.AppendLine("}");
    }

    private static void AddDisposePart(LinesBuilder code)
    {
        code.AppendLine($"var disposableInstance = instance as {Names.IDisposableInterfaceName};");
        code.AppendLine("if (disposableInstance != null)");
        code.AppendLine("{");
        using (code.Indent())
        {
            code.AppendLine("try");
            code.AppendLine("{");
            using (code.Indent())
            {
                code.AppendLine("disposableInstance.Dispose();");
            }

            code.AppendLine("}");
            code.AppendLine("catch (Exception exception)");
            code.AppendLine("{");
            using (code.Indent())
            {
                code.AppendLine($"{Names.OnDisposeExceptionMethodName}(disposableInstance, exception);");
            }

            code.AppendLine("}");
            code.AppendLine("continue;");
        }

        code.AppendLine("}");
    }

    private static void AddSyncPart(CompositionCode composition, LinesBuilder code)
    {
        code.AppendLine("int disposeIndex;");
        code.AppendLine("object[] disposables;");
        code.AppendLine($"lock ({Names.LockFieldName})");
        code.AppendLine("{");
        using (code.Indent())
        {
            code.AppendLine($"disposeIndex = {Names.DisposeIndexFieldName};");
            code.AppendLine($"{Names.DisposeIndexFieldName} = 0;");
            code.AppendLine($"disposables = {Names.DisposablesFieldName};");
            code.AppendLine($"{Names.DisposablesFieldName} = new object[{composition.TotalDisposablesCount.ToString()}];");
            foreach (var singletonField in composition.Singletons)
            {
                code.AppendLine(
                    singletonField.InstanceType.IsValueType
                        ? $"{singletonField.VariableName}Created = false;"
                        : $"{singletonField.VariableName} = null;");
            }
        }
        code.AppendLine("}");
        code.AppendLine();
    }
}