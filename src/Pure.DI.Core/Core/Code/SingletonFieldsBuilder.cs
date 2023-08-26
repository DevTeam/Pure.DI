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
        
        // Singleton fields
        foreach (var singletonField in composition.Singletons)
        {
            code.AppendLine($"private {singletonField.InstanceType} {singletonField.VarName};");
            membersCounter++;

            if (!singletonField.InstanceType.IsValueType)
            {
                continue;
            }

            code.AppendLine($"private bool {singletonField.VarName}Created;");
            membersCounter++;
        }

        return composition with { MembersCount = membersCounter };
    }
}