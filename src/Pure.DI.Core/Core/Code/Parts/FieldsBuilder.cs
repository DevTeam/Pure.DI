// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable InvertIf

namespace Pure.DI.Core.Code.Parts;

internal sealed class FieldsBuilder(
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

        // _parent filed
        code.AppendLine($"private readonly {composition.Source.Source.Name.ClassName} {Names.RootFieldName};");
        membersCounter++;

        if (composition.IsThreadSafe)
        {
            // _lock field
            code.AppendLine($"private readonly {locks.GetLockType(composition.Source.Source)} {Names.LockFieldName};");
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
                    code.AppendLine($"private {typeResolver.Resolve(composition.Source.Source, singletonField.InstanceType)} {singletonField.VariableDeclarationName};");
                    membersCounter++;

                    code.AppendLine($"private bool {singletonField.VariableDeclarationName}Created;");
                }
                else
                {
                    code.AppendLine($"private {typeResolver.Resolve(composition.Source.Source, singletonField.InstanceType)}{nullable} {singletonField.VariableDeclarationName};");
                }

                membersCounter++;
            }
        }

        return composition with { MembersCount = membersCounter };
    }
}