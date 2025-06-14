// ReSharper disable ClassNeverInstantiated.Global

namespace Pure.DI.Core.Code.Parts;

using static LinesBuilderExtensions;

sealed class DisposeMethodBuilder(
    ILocks locks)
    : IClassPartBuilder
{
    public ClassPart Part => ClassPart.DisposeMethod;

    public CompositionCode Build(CompositionCode composition)
    {
        var code = composition.Code;
        var membersCounter = composition.MembersCount;
        if (composition.TotalDisposablesCount == 0)
        {
            return composition with { MembersCount = membersCounter };
        }

        var hasDisposable = composition.DisposablesCount > 0;
        var hasAsyncDisposable = composition.AsyncDisposableCount > 0;
        var hints = composition.Source.Source.Hints;
        var isCommentsEnabled = hints.IsCommentsEnabled;
        if (isCommentsEnabled)
        {
            code.AppendLine("/// <summary>");
            code.AppendLine("/// <inheritdoc/>");
            code.AppendLine("/// </summary>");
        }

        code.AppendLine($"{composition.Source.Source.Hints.DisposeMethodModifiers} void Dispose()");
        using (code.CreateBlock())
        {
            AddSyncPart(composition, code, false);
            code.AppendLine();
            code.AppendLine("while (disposeIndex-- > 0)");
            using (code.CreateBlock())
            {
                code.AppendLine("switch (disposables[disposeIndex])");
                using (code.CreateBlock())
                {
                    if (hasDisposable)
                    {
                        AddDisposePart(code);
                    }

                    if (hasAsyncDisposable)
                    {
                        if (hasDisposable)
                        {
                            code.AppendLine();
                        }

                        AddDisposeAsyncPart(code, false);
                    }
                }
            }
        }

        membersCounter++;

        code.AppendLine();
        code.AppendLine("/// <summary>");
        code.AppendLine("/// Implement this partial method to handle the exception on disposing.");
        code.AppendLine("/// </summary>");
        code.AppendLine("/// <param name=\"disposableInstance\">The disposable instance.</param>");
        code.AppendLine("/// <param name=\"exception\">Exception occurring during disposal.</param>");
        code.AppendLine("/// <typeparam name=\"T\">The actual type of instance being disposed of.</typeparam>");
        code.AppendLine($"partial void {Names.OnDisposeExceptionMethodName}<T>(T disposableInstance, {Names.ExceptionTypeName} exception) where T : {Names.IDisposableTypeName};");
        membersCounter++;

        // ReSharper disable once InvertIf
        if (hasAsyncDisposable)
        {
            code.AppendLine();
            if (isCommentsEnabled)
            {
                code.AppendLine("/// <summary>");
                code.AppendLine("/// <inheritdoc/>");
                code.AppendLine("/// </summary>");
            }

            code.AppendLine($"{composition.Source.Source.Hints.DisposeAsyncMethodModifiers} async {Names.ValueTaskTypeName} DisposeAsync()");
            using (code.CreateBlock())
            {
                AddSyncPart(composition, code, true);
                code.AppendLine();
                code.AppendLine("while (disposeIndex-- > 0)");
                using (code.CreateBlock())
                {
                    code.AppendLine("switch (disposables[disposeIndex])");
                    using (code.CreateBlock())
                    {
                        if (hasAsyncDisposable)
                        {
                            AddDisposeAsyncPart(code, true);
                        }

                        if (hasDisposable)
                        {
                            if (hasAsyncDisposable)
                            {
                                code.AppendLine();
                            }

                            AddDisposePart(code);
                        }
                    }
                }
            }

            membersCounter++;

            code.AppendLine();
            code.AppendLine("/// <summary>");
            code.AppendLine("/// Implement this partial method to handle the exception on async disposing.");
            code.AppendLine("/// </summary>");
            code.AppendLine("/// <param name=\"asyncDisposableInstance\">The disposable instance.</param>");
            code.AppendLine("/// <param name=\"exception\">Exception occurring during disposal.</param>");
            code.AppendLine("/// <typeparam name=\"T\">The actual type of instance being disposed of.</typeparam>");
            code.AppendLine($"partial void {Names.OnDisposeAsyncExceptionMethodName}<T>(T asyncDisposableInstance, {Names.ExceptionTypeName} exception) where T : {Names.IAsyncDisposableTypeName};");
            membersCounter++;
        }

        return composition with { MembersCount = membersCounter };
    }

    private static void AddDisposeAsyncPart(LinesBuilder code, bool makeAsyncCall)
    {
        code.AppendLine($"case {Names.IAsyncDisposableTypeName} asyncDisposableInstance:");
        using (code.Indent())
        {
            code.AppendLine("try");
            using (code.CreateBlock())
            {
                if (makeAsyncCall)
                {
                    code.AppendLine("await asyncDisposableInstance.DisposeAsync();");
                }
                else
                {
                    code.AppendLine("var valueTask = asyncDisposableInstance.DisposeAsync();");
                    code.AppendLine("if (!valueTask.IsCompleted)");
                    using (code.CreateBlock())
                    {
                        code.AppendLine("valueTask.AsTask().Wait();");
                    }
                }
            }

            code.AppendLine($"catch ({Names.ExceptionTypeName} exception)");
            using (code.CreateBlock())
            {
                code.AppendLine($"{Names.OnDisposeAsyncExceptionMethodName}(asyncDisposableInstance, exception);");
            }

            code.AppendLine("break;");
        }
    }

    private static void AddDisposePart(LinesBuilder code)
    {
        code.AppendLine($"case {Names.IDisposableTypeName} disposableInstance:");
        using (code.Indent())
        {
            code.AppendLine("try");
            using (code.CreateBlock())
            {
                code.AppendLine("disposableInstance.Dispose();");
            }

            code.AppendLine($"catch ({Names.ExceptionTypeName} exception)");
            using (code.CreateBlock())
            {
                code.AppendLine($"{Names.OnDisposeExceptionMethodName}(disposableInstance, exception);");
            }

            code.AppendLine("break;");
        }
    }

    private void AddSyncPart(CompositionCode composition, LinesBuilder code, bool isAsync)
    {
        code.AppendLine("int disposeIndex;");
        code.AppendLine("object[] disposables;");
        if (composition.IsThreadSafe)
        {
            locks.AddLockStatements(composition.Source, code, isAsync);
            code.AppendLine(BlockStart);
            code.IncIndent();
        }

        code.AppendLine($"disposeIndex = {Names.DisposeIndexFieldName};");
        code.AppendLine($"{Names.DisposeIndexFieldName} = 0;");
        code.AppendLine($"disposables = {Names.DisposablesFieldName};");
        code.AppendLine($"{Names.DisposablesFieldName} = new object[{composition.TotalDisposablesCount.ToString()}];");
        foreach (var singletonField in composition.Singletons)
        {
            code.AppendLine(
                singletonField.InstanceType.IsValueType
                    ? $"{singletonField.VariableDeclarationName}Created = false;"
                    : $"{singletonField.VariableDeclarationName} = null;");
        }

        // ReSharper disable once InvertIf
        if (composition.IsThreadSafe)
        {
            code.AppendLine(BlockFinish);
            locks.AddUnlockStatements(composition.Source, code, isAsync);
        }
    }
}