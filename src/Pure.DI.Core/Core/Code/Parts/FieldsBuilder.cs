// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable InvertIf

namespace Pure.DI.Core.Code.Parts;

sealed class FieldsBuilder(
    ITypeResolver typeResolver,
    ILocks locks)
    : IClassPartBuilder
{
    public ClassPart Part => ClassPart.Fields;

    public CompositionCode Build(CompositionCode composition)
    {
        var code = composition.Code;
        var membersCounter = composition.MembersCount;
        var compilation = composition.Compilation;
        var nullable = compilation.Options.NullableContextOptions == NullableContextOptions.Disable ? "" : "?";

        if (composition.Singletons.Length > 0)
        {
            // _parent filed
            code.AppendLine($"private readonly {composition.Source.Source.Name.ClassName} {Names.RootFieldName};");
            membersCounter++;
        }

        if (composition.IsThreadSafe || locks.HasLockField(composition.Source))
        {
            // _lock field
            code.AppendLine(new Line(int.MinValue, "#if NET9_0_OR_GREATER"));
            code.AppendLine($"private readonly {Names.LockTypeName} {Names.LockFieldName};");
            code.AppendLine(new Line(int.MinValue, "#else"));
            code.AppendLine($"private readonly {Names.ObjectTypeName} {Names.LockFieldName};");
            code.AppendLine(new Line(int.MinValue, "#endif"));
            membersCounter++;
        }

        if (composition.TotalDisposablesCount > 0)
        {
            // _disposables field
            code.AppendLine($"private object[] {Names.DisposablesFieldName};");
            membersCounter++;

            // _disposeIndex field
            code.AppendLine($"private int {Names.DisposeIndexFieldName};");
            membersCounter++;
        }

        // Singleton fields
        if (composition.Singletons.Length > 0)
        {
            code.AppendLine();
            foreach (var singletonField in composition.Singletons)
            {
                if (singletonField.InstanceType.IsValueType)
                {
                    code.AppendLine($"private {typeResolver.Resolve(composition.Source.Source, singletonField.InstanceType)} {singletonField.Name};");
                    membersCounter++;

                    code.AppendLine($"private bool {singletonField.Name}Created;");
                }
                else
                {
                    code.AppendLine($"private {typeResolver.Resolve(composition.Source.Source, singletonField.InstanceType)}{nullable} {singletonField.Name};");
                }

                membersCounter++;
            }
        }

        return composition with { MembersCount = membersCounter };
    }
}