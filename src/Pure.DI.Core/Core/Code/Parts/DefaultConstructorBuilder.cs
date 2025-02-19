// ReSharper disable ClassNeverInstantiated.Global

namespace Pure.DI.Core.Code.Parts;

sealed class DefaultConstructorBuilder(
    ILocks locks)
    : IClassPartBuilder
{
    public ClassPart Part => ClassPart.DefaultConstructor;

    public CompositionCode Build(CompositionCode composition)
    {
        if (composition.Args.Length > 0)
        {
            return composition;
        }

        var code = composition.Code;
        var membersCounter = composition.MembersCount;
        var hints = composition.Source.Source.Hints;
        var isCommentsEnabled = hints.IsCommentsEnabled;
        if (isCommentsEnabled)
        {
            code.AppendLine("/// <summary>");
            code.AppendLine($"/// This constructor creates a new instance of <see cref=\"{composition.Source.Source.Name.ClassName}\"/>.");
            code.AppendLine("/// </summary>");
        }

        code.AppendLine($"[{Names.OrdinalAttributeName}(256)]");
        code.AppendLine($"public {composition.Source.Source.Name.ClassName}()");
        using (code.CreateBlock())
        {
            code.AppendLine($"{Names.RootFieldName} = this;");
            if (composition.IsThreadSafe)
            {
                code.AppendLine($"{Names.LockFieldName} = new {locks.GetLockType(composition.Source.Source)}();");
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