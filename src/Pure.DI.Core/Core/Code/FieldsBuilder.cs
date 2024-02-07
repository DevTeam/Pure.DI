// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core.Code;

internal sealed class FieldsBuilder: IBuilder<CompositionCode, CompositionCode>
{
    public CompositionCode Build(CompositionCode composition)
    {
        var code = composition.Code;
        var membersCounter = composition.MembersCount;
        
        // Lock field
        code.AppendLine($"private readonly object {Names.LockFieldName};");
        
        // Disposables field
        if (composition.DisposableSingletonsCount > 0)
        {
            code.AppendLine($"private readonly {Names.IDisposableInterfaceName}[] {Names.DisposablesFieldName};");
            membersCounter++;
        }

        if (composition.Singletons.Length == 0)
        {
            return composition with { MembersCount = membersCounter };
        }
        
        if (composition.DisposableSingletonsCount > 0)
        {
            // DisposeIndex field
            code.AppendLine($"private int {Names.DisposeIndexFieldName};");
            membersCounter++;
        }

        // Singleton fields
        foreach (var singletonField in composition.Singletons)
        {
            if (singletonField.InstanceType.IsValueType)
            {
                code.AppendLine($"private {singletonField.InstanceType} {singletonField.VariableName};");
                membersCounter++;

                code.AppendLine($"private bool {singletonField.VariableName}Created;");
                membersCounter++;
            }
            else
            {
                code.AppendLine($"private {singletonField.InstanceType} {singletonField.VariableName};");
                membersCounter++;
            }
        }

        return composition with { MembersCount = membersCounter };
    }
}