// ReSharper disable ClassNeverInstantiated.Global

namespace Pure.DI.Core.Code.Parts;

sealed class DefaultConstructorBuilder(
    IConstructors constructors,
    ICodeNameProvider codeNameProvider)
    : IClassPartBuilder
{
    public ClassPart Part => ClassPart.DefaultConstructor;

    public CompositionCode Build(CompositionCode composition)
    {
        if (!constructors.IsEnabled(composition, ConstructorKind.Default) || composition.IsScopeMethod)
        {
            return composition;
        }

        var code = composition.Code;
        var membersCounter = composition.MembersCount;
        var hints = composition.Hints;
        var isCommentsEnabled = hints.IsCommentsEnabled;
        if (isCommentsEnabled)
        {
            code.AppendLine("/// <summary>");
            code.AppendLine($"/// This constructor creates a new instance of <see cref=\"{composition.Name.ClassName}\"/>. Typically used to create a root scope in scenarios where <see cref=\"Lifetime.Scoped\"/> lifetime might be used.");
            code.AppendLine("/// </summary>");
        }

        code.AppendLine($"[{Names.OrdinalAttributeName}(256)]");
        var ctorName = codeNameProvider.GetConstructorName(composition.Name.ClassName);
        code.AppendLine($"public {ctorName}()");
        using (code.CreateBlock())
        {
            if (composition.Singletons.Length > 0)
            {
                code.AppendLine($"{Names.RootFieldName} = this;");
            }

            if (composition.IsLockRequired)
            {
                code.AppendLine(new Line(int.MinValue, "#if NET9_0_OR_GREATER"));
                code.AppendLine($"{Names.LockFieldName} = new {Names.LockTypeName}();");
                code.AppendLine(new Line(int.MinValue, "#else"));
                code.AppendLine($"{Names.LockFieldName} = new {Names.ObjectTypeName}();");
                code.AppendLine(new Line(int.MinValue, "#endif"));
            }

            if (composition.TotalDisposablesCount > 0)
            {
                code.AppendLine($"{Names.DisposablesFieldName} = new object[{composition.TotalDisposablesCount.ToString()}];");
            }
        }

        membersCounter++;
        return composition with { MembersCount = membersCounter };
    }

}
