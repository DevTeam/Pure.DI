// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core.Code;

internal sealed class SingletonFieldsBuilder: IBuilder<CompositionCode, CompositionCode>
{
    public CompositionCode Build(CompositionCode composition)
    {
        var code = composition.Code;
        var membersCounter = composition.MembersCount;
        
        // Disposables field
        code.AppendLine($"private readonly {Names.IDisposableInterfaceName}[] {Names.DisposablesFieldName};");
        membersCounter++;
        
        if (!composition.Singletons.Any())
        {
            return composition with { MembersCount = membersCounter };
        }
        
        if (composition.DisposableSingletonsCount > 0)
        {
            // DisposeIndex field
            code.AppendLine($"private int {Names.DisposeIndexFieldName};");
            membersCounter++;
        }

        var fieldModifiers =
            composition.Source.Source.Hints.GetHint(Hint.ThreadSafe, SettingState.On) == SettingState.On
                ? "volatile "
                : "";
        
        // Singleton fields
        foreach (var singletonField in composition.Singletons)
        {
            if (singletonField.InstanceType.IsValueType)
            {
                code.AppendLine($"private {(singletonField.InstanceType.IsAtomicValueType() ? fieldModifiers : "")}{singletonField.InstanceType} {singletonField.VarName};");
                membersCounter++;

                code.AppendLine($"private {fieldModifiers}bool {singletonField.VarName}Created;");
                membersCounter++;
            }
            else
            {
                code.AppendLine($"private {fieldModifiers}{singletonField.InstanceType} {singletonField.VarName};");
                membersCounter++;
            }
        }

        return composition with { MembersCount = membersCounter };
    }
}