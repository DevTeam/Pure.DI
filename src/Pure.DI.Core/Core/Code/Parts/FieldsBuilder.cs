// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable InvertIf

namespace Pure.DI.Core.Code.Parts;

sealed class FieldsBuilder(
    ITypeResolver typeResolver,
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
        var hasScopeFactory = isAnyConstructorEnabled && constructors.IsEnabled(composition, ConstructorKind.Scope) && composition.IsFactoryMethod;
        var skipFieldsInit = isAnyConstructorEnabled && !hasScopeFactory;
        if (isAnyConstructorEnabled && composition.Singletons.Length > 0)
        {
            // _parent filed
            code.AppendLine($"[{Names.NonSerializedAttributeTypeName}] private {composition.Name.ClassName} {Names.RootFieldName};");
            membersCounter++;
        }

        if (composition.IsLockRequired)
        {
            // _lock field
            code.AppendLine(new Line(int.MinValue, "#if NET9_0_OR_GREATER"));
            code.AppendLine($"[{Names.NonSerializedAttributeTypeName}] private {(composition.IsFactoryMethod ? "" : "readonly ")}{Names.LockTypeName} {Names.LockFieldName}{(skipFieldsInit ? "" : $" = new {Names.LockTypeName}()")};");
            code.AppendLine(new Line(int.MinValue, "#else"));
        code.AppendLine($"[{Names.NonSerializedAttributeTypeName}] private {(composition.IsFactoryMethod ? "" : "readonly ")}{Names.ObjectTypeName} {Names.LockFieldName}{(skipFieldsInit ? "" : $" = new {Names.ObjectTypeName}()")};");
            code.AppendLine(new Line(int.MinValue, "#endif"));
            membersCounter++;
        }

        if (composition.TotalDisposablesCount > 0)
        {
            // _disposables field
            code.AppendLine($"[{Names.NonSerializedAttributeTypeName}] private object[] {Names.DisposablesFieldName}{(skipFieldsInit ? "" : $" = new object[{composition.TotalDisposablesCount.ToString()}]")};");
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
                    code.AppendLine($"[{Names.NonSerializedAttributeTypeName}] private {typeResolver.Resolve(composition.Setup, singletonField.InstanceType)} {singletonField.Name};");
                    membersCounter++;

                    code.AppendLine($"[{Names.NonSerializedAttributeTypeName}] private bool {singletonField.Name}{Names.CreatedValueNameSuffix};");
                }
                else
                {
                    code.AppendLine($"[{Names.NonSerializedAttributeTypeName}] private {typeResolver.Resolve(composition.Setup, singletonField.InstanceType)}{nullable} {singletonField.Name};");
                }

                membersCounter++;
            }
        }

        return composition with { MembersCount = membersCounter };
    }
}
