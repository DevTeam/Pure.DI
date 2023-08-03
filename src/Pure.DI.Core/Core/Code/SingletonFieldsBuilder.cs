// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core.Code;

internal sealed class SingletonFieldsBuilder: IBuilder<CompositionCode, CompositionCode>
{
    public CompositionCode Build(CompositionCode composition)
    {
        if (!composition.Singletons.Any())
        {
            return composition;
        }
        
        var code = composition.Code;
        var membersCounter = composition.MembersCount;
        if (composition.DisposableSingletonsCount > 0)
        {
            // DisposeIndex field
            code.AppendLine($"private int {Variable.DisposeIndexFieldName};");
            membersCounter++;
        }
            
        // Disposables field
        code.AppendLine($"private readonly {Constant.IDisposableInterfaceName}[] {Variable.DisposablesFieldName};");
        membersCounter++;

        // Singleton fields
        foreach (var singletonField in composition.Singletons)
        {
            code.AppendLine($"private {singletonField.InstanceType} {singletonField.Name};");
            membersCounter++;

            if (!singletonField.InstanceType.IsValueType)
            {
                continue;
            }

            code.AppendLine($"private bool {singletonField.Name}Created;");
            membersCounter++;
        }

        return composition with { MembersCount = membersCounter };
    }
}