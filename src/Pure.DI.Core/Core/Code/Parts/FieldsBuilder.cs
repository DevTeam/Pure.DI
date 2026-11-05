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
        var isNullableEnabled = compilation.Options.NullableContextOptions != NullableContextOptions.Disable;
        var isAnyConstructorEnabled = constructors.IsEnabled(composition.Source);
        var skipFieldsInit = isAnyConstructorEnabled && !composition.IsScopeMethod;
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
            code.AppendLine($"[{Names.NonSerializedAttributeTypeName}] private {(composition.IsScopeMethod ? "" : "readonly ")}{Names.LockTypeName} {Names.LockFieldName}{(skipFieldsInit ? "" : $" = new {Names.LockTypeName}()")};");
            code.AppendLine(new Line(int.MinValue, "#else"));
        code.AppendLine($"[{Names.NonSerializedAttributeTypeName}] private {(composition.IsScopeMethod ? "" : "readonly ")}{Names.ObjectTypeName} {Names.LockFieldName}{(skipFieldsInit ? "" : $" = new {Names.ObjectTypeName}()")};");
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

            if (composition.DisposablesScopedCount > 0)
            {
                // A scope creates its array of disposables on the first scoped disposable instance only.
                code.AppendLine($"private const int {Names.ScopedDisposablesCountConstName} = {composition.DisposablesScopedCount.ToString()};");
                membersCounter++;
            }
        }

        // Singleton fields
        var hasSharedSingletons = constructors.HasShared(composition);
        if (hasSharedSingletons)
        {
            // _singletons field
            code.AppendLine($"[{Names.NonSerializedAttributeTypeName}] private {Names.SingletonsClassName} {Names.SingletonsFieldName};");
            membersCounter++;
        }

        if (composition.Singletons.Length > 0)
        {
            code.AppendLine();
            foreach (var singletonField in composition.Singletons)
            {
                if (!constructors.IsShared(composition, singletonField))
                {
                    membersCounter += AddSingletonField(composition, code, singletonField, "private", isNullableEnabled);
                }
            }
        }

        // ReSharper disable once InvertIf
        if (hasSharedSingletons)
        {
            code.AppendLine();
            code.AppendLine($"private sealed class {Names.SingletonsClassName}");
            using (code.CreateBlock())
            {
                foreach (var singletonField in composition.Singletons)
                {
                    if (constructors.IsShared(composition, singletonField))
                    {
                        AddSingletonField(composition, code, singletonField, "internal", isNullableEnabled);
                    }
                }
            }

            membersCounter++;
        }

        return composition with { MembersCount = membersCounter };
    }

    private int AddSingletonField(CompositionCode composition, Lines code, VarDeclaration singletonField, string modifier, bool isNullableEnabled)
    {
        var type = typeResolver.Resolve(composition.Setup, singletonField.InstanceType);
        if (singletonField.InstanceType.IsValueType)
        {
            code.AppendLine($"[{Names.NonSerializedAttributeTypeName}] {modifier} {type} {singletonField.Name};");
            code.AppendLine($"[{Names.NonSerializedAttributeTypeName}] {modifier} bool {singletonField.Name}{Names.CreatedValueNameSuffix};");
            return 2;
        }

        var nullable = isNullableEnabled && singletonField.InstanceType.NullableAnnotation != NullableAnnotation.Annotated ? "?" : "";
        code.AppendLine($"[{Names.NonSerializedAttributeTypeName}] {modifier} {type}{nullable} {singletonField.Name};");
        return 1;
    }
}
