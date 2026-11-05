// ReSharper disable ClassNeverInstantiated.Global

namespace Pure.DI.Core.Code.Parts;

using static LinesExtensions;

sealed class DisposeMethodBuilder(
    ITypeResolver typeResolver,
    ILocks locks,
    IConstructors constructors,
    ISharedSingletons sharedSingletons)
    : IClassPartBuilder
{
    public ClassPart Part => ClassPart.DisposeMethod;

    public CompositionCode Build(CompositionCode composition)
    {
        var code = composition.Code;
        var membersCounter = composition.MembersCount;
        var hints = composition.Hints;
        var isCommentsEnabled = hints.IsCommentsEnabled;
        var isOnDisposeEnabled = hints.IsOnDisposeEnabled;
        var isOnDisposeAsyncEnabled = hints.IsOnDisposeAsyncEnabled;

        // Emit the OnDispose / OnDisposeAsync defining declarations before any early-return
        // so that user-defined implementing declarations always resolve, even when the
        // composition has no tracked disposable instances.
        if (isOnDisposeEnabled)
        {
            code.AppendLine("/// <summary>");
            code.AppendLine("/// Implement this partial method to customize the disposal of tracked instances.");
            code.AppendLine("/// Return <c>true</c> when the instance has already been disposed of by the hook");
            code.AppendLine("/// (for example via a Close-with-timeout / Abort fallback) and the default");
            code.AppendLine("/// <see cref=\"global::System.IDisposable.Dispose\"/> call must be skipped;");
            code.AppendLine("/// return <c>false</c> to let the composition invoke the default disposal afterwards.");
            code.AppendLine("/// </summary>");
            code.AppendLine("/// <param name=\"disposableInstance\">The disposable instance to be disposed.</param>");
            code.AppendLine("/// <typeparam name=\"T\">The actual type of instance being disposed of.</typeparam>");
            code.AppendLine($"private partial bool {Names.OnDisposeMethodName}<T>(in T disposableInstance) where T : {Names.IDisposableTypeName};");
            membersCounter++;
        }

        if (isOnDisposeAsyncEnabled)
        {
            code.AppendLine();
            code.AppendLine("/// <summary>");
            code.AppendLine("/// Implement this partial method to customize the async disposal of tracked instances.");
            code.AppendLine("/// Return <c>true</c> when the instance has already been disposed of by the hook");
            code.AppendLine("/// (for example via a Close-with-timeout / Abort fallback) and the default");
            code.AppendLine("/// <see cref=\"global::System.IAsyncDisposable.DisposeAsync\"/> call must be skipped;");
            code.AppendLine("/// return <c>false</c> to let the composition invoke the default disposal afterwards.");
            code.AppendLine("/// </summary>");
            code.AppendLine("/// <param name=\"asyncDisposableInstance\">The async disposable instance to be disposed.</param>");
            code.AppendLine("/// <typeparam name=\"T\">The actual type of instance being disposed of.</typeparam>");
            code.AppendLine($"private partial {Names.ValueTaskTypeName}<bool> {Names.OnDisposeAsyncMethodName}<T>(in T asyncDisposableInstance) where T : {Names.IAsyncDisposableTypeName};");
            membersCounter++;
        }

        if (composition.TotalDisposablesCount == 0)
        {
            return composition with { MembersCount = membersCounter };
        }

        var hasDisposable = composition.DisposablesCount > 0;
        var hasAsyncDisposable = composition.AsyncDisposableCount > 0;
        if (isCommentsEnabled)
        {
            code.AppendLine("/// <summary>");
            code.AppendLine("/// Disposes tracked singleton and scoped instances created by this composition.");
            code.AppendLine("/// </summary>");
        }

        code.AppendLine($"{composition.Hints.DisposeMethodModifiers} void Dispose()");
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
                        AddDisposePart(code, isOnDisposeEnabled);
                    }

                    if (hasAsyncDisposable)
                    {
                        if (hasDisposable)
                        {
                            code.AppendLine();
                        }

                        AddDisposeAsyncPart(code, false, isOnDisposeAsyncEnabled);
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
            code.AppendLine("/// <summary>");
            code.AppendLine("/// Implement this partial method to handle the exception on async disposing.");
            code.AppendLine("/// </summary>");
            code.AppendLine("/// <param name=\"asyncDisposableInstance\">The disposable instance.</param>");
            code.AppendLine("/// <param name=\"exception\">Exception occurring during disposal.</param>");
            code.AppendLine("/// <typeparam name=\"T\">The actual type of instance being disposed of.</typeparam>");
            code.AppendLine($"partial void {Names.OnDisposeAsyncExceptionMethodName}<T>(T asyncDisposableInstance, {Names.ExceptionTypeName} exception) where T : {Names.IAsyncDisposableTypeName};");
            membersCounter++;
        }

        if (hasAsyncDisposable)
        {
            if (isCommentsEnabled)
            {
                code.AppendLine("/// <summary>");
                code.AppendLine("/// Asynchronously disposes tracked singleton and scoped instances created by this composition.");
                code.AppendLine("/// </summary>");
            }

            code.AppendLine($"{composition.Hints.DisposeAsyncMethodModifiers} async {Names.ValueTaskTypeName} DisposeAsync()");
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
                            AddDisposeAsyncPart(code, true, isOnDisposeAsyncEnabled);
                        }

                        if (hasDisposable)
                        {
                            if (hasAsyncDisposable)
                            {
                                code.AppendLine();
                            }

                            AddDisposePart(code, isOnDisposeEnabled);
                        }
                    }
                }
            }

            membersCounter++;
        }

        return composition with { MembersCount = membersCounter };
    }

    private static void AddDisposeAsyncPart(Lines code, bool makeAsyncCall, bool isHookEnabled)
    {
        code.AppendLine($"case {Names.IAsyncDisposableTypeName} asyncDisposableInstance:");
        using (code.Indent())
        {
            code.AppendLine("try");
            using (code.CreateBlock())
            {
                if (isHookEnabled)
                {
                    code.AppendLine("if (!" + (makeAsyncCall ? "await " : "") + Names.OnDisposeAsyncMethodName + "(in asyncDisposableInstance)" + (makeAsyncCall ? "" : ".GetAwaiter().GetResult()") + ")");
                    using (code.Indent())
                    {
                        code.AppendLine(makeAsyncCall ? "await asyncDisposableInstance.DisposeAsync();" : "asyncDisposableInstance.DisposeAsync().GetAwaiter().GetResult();");
                    }
                }
                else
                {
                    code.AppendLine(makeAsyncCall ? "await asyncDisposableInstance.DisposeAsync();" : "asyncDisposableInstance.DisposeAsync().GetAwaiter().GetResult();");
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

    private static void AddDisposePart(Lines code, bool isHookEnabled)
    {
        code.AppendLine($"case {Names.IDisposableTypeName} disposableInstance:");
        using (code.Indent())
        {
            code.AppendLine("try");
            using (code.CreateBlock())
            {
                if (isHookEnabled)
                {
                    code.AppendLine($"if (!{Names.OnDisposeMethodName}(in disposableInstance))");
                    using (code.Indent())
                    {
                        code.AppendLine("disposableInstance.Dispose();");
                    }
                }
                else
                {
                    code.AppendLine("disposableInstance.Dispose();");
                }
            }

            code.AppendLine($"catch ({Names.ExceptionTypeName} exception)");
            using (code.CreateBlock())
            {
                code.AppendLine($"{Names.OnDisposeExceptionMethodName}(disposableInstance, exception);");
            }

            code.AppendLine("break;");
        }
    }

    private void AddSyncPart(CompositionCode composition, Lines code, bool isAsync)
    {
        code.AppendLine("int disposeIndex;");
        code.AppendLine("object[] disposables;");
        var isLockRequired = composition.IsLockRequired;
        if (isLockRequired)
        {
            locks.AddLockStatements(false, code, isAsync);
            code.AppendLine(BlockStart);
            code.IncIndent();
        }

        code.AppendLine($"disposeIndex = {Names.DisposeIndexFieldName};");
        code.AppendLine($"{Names.DisposeIndexFieldName} = 0;");
        code.AppendLine($"disposables = {Names.DisposablesFieldName};");
        if (constructors.IsEnabled(composition, ConstructorKind.Scope))
        {
            // Only the root composition keeps an array of disposables after disposal.
            // A scope creates a new one on its next scoped disposable instance.
            code.AppendLine($"{Names.DisposablesFieldName} = {Names.RootFieldName} == null || {Names.ObjectTypeName}.ReferenceEquals({Names.RootFieldName}, this) ? new object[{composition.TotalDisposablesCount.ToString()}] : null;");
        }
        else
        {
            code.AppendLine($"{Names.DisposablesFieldName} = new object[{composition.TotalDisposablesCount.ToString()}];");
        }
        foreach (var singletonField in composition.Singletons)
        {
            if (!sharedSingletons.IsShared(composition, singletonField))
            {
                AddResetField(composition, code, singletonField, "");
            }
        }

        if (sharedSingletons.HasShared(composition))
        {
            // Only the root composition releases the singletons it shares with its scopes.
            var isScopeEnabled = constructors.IsEnabled(composition, ConstructorKind.Scope);
            code.AppendLine(isScopeEnabled
                ? $"if ({Names.SingletonsFieldName} != null && ({Names.RootFieldName} == null || {Names.ObjectTypeName}.ReferenceEquals({Names.RootFieldName}, this)))"
                : $"if ({Names.SingletonsFieldName} != null)");
            using (code.CreateBlock())
            {
                foreach (var singletonField in composition.Singletons)
                {
                    if (sharedSingletons.IsShared(composition, singletonField))
                    {
                        AddResetField(composition, code, singletonField, $"{Names.SingletonsFieldName}.");
                    }
                }
            }
        }

        // ReSharper disable once InvertIf
        if (isLockRequired)
        {
            code.DecIndent();
            code.AppendLine(BlockFinish);
        }
    }

    private void AddResetField(CompositionCode composition, Lines code, VarDeclaration singletonField, string owner)
    {
        if (singletonField.InstanceType.IsValueType)
        {
            code.AppendLine($"{owner}{singletonField.Name} = default({typeResolver.Resolve(composition.Setup, singletonField.InstanceType)});");
            code.AppendLine($"{owner}{singletonField.Name}{Names.CreatedValueNameSuffix} = false;");
        }
        else
        {
            code.AppendLine($"{owner}{singletonField.Name} = null;");
        }
    }
}
