// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable InvertIf

namespace Pure.DI.Core.Code.Parts;

sealed class FieldsBuilder(
    ITypeResolver typeResolver,
    ILocks locks,
    IConstructors constructors)
    : IClassPartBuilder
{
    public ClassPart Part => ClassPart.Fields;

    public CompositionCode Build(CompositionCode composition)
    {
        var code = composition.Code;
        var membersCounter = composition.MembersCount;
        var compilation = composition.Compilation;
        var nullable = compilation.Options.NullableContextOptions == NullableContextOptions.Disable ? "" : "?";

        var isAnyConstructorEnabled = constructors.IsEnabled(composition.Source);
        if (isAnyConstructorEnabled && composition.Singletons.Length > 0)
        {
            // _parent filed
            code.AppendLine($"private readonly {composition.Source.Source.Name.ClassName} {Names.RootFieldName};");
            membersCounter++;
        }

        if (composition.IsLockRequired(locks))
        {
            // _lock field
            code.AppendLine(new Line(int.MinValue, "#if NET9_0_OR_GREATER"));
            code.AppendLine($"[{Names.NonSerializedAttributeTypeName}] private readonly {Names.LockTypeName} {Names.LockFieldName}{(isAnyConstructorEnabled ? "" : $" = new {Names.LockTypeName}()")};");
            code.AppendLine(new Line(int.MinValue, "#else"));
            code.AppendLine($"[{Names.NonSerializedAttributeTypeName}] private readonly {Names.ObjectTypeName} {Names.LockFieldName}{(isAnyConstructorEnabled ? "" : $" = new {Names.ObjectTypeName}()")};");
            code.AppendLine(new Line(int.MinValue, "#endif"));
            membersCounter++;
        }

        if (composition.TotalDisposablesCount > 0)
        {
            // _disposables field
            code.AppendLine($"[{Names.NonSerializedAttributeTypeName}] private object[] {Names.DisposablesFieldName}{(isAnyConstructorEnabled ? "" : $" = new object[{composition.TotalDisposablesCount.ToString()}]")};");
            membersCounter++;

            // _disposeIndex field
            code.AppendLine($"[{Names.NonSerializedAttributeTypeName}] private int {Names.DisposeIndexFieldName};");
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
                    code.AppendLine($"[{Names.NonSerializedAttributeTypeName}] private {typeResolver.Resolve(composition.Source.Source, singletonField.InstanceType)} {singletonField.Name};");
                    membersCounter++;

                    code.AppendLine($"[{Names.NonSerializedAttributeTypeName}] private bool {singletonField.Name}{Names.CreatedValueNameSuffix};");
                }
                else
                {
                    code.AppendLine($"[{Names.NonSerializedAttributeTypeName}] private {typeResolver.Resolve(composition.Source.Source, singletonField.InstanceType)}{nullable} {singletonField.Name};");
                }

                membersCounter++;
            }
        }

        return composition with { MembersCount = membersCounter };
    }
}