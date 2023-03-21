namespace Pure.DI.Core.CSharp;

internal class CodeSingletonFieldsBuilder: IBuilder<ComposerCode, ComposerCode>
{
    public ComposerCode Build(ComposerCode composer, CancellationToken cancellationToken)
    {
        var code = composer.Code;
        var membersCounter = composer.MembersCount;
        if (!composer.Singletons.Any())
        {
            return composer;
        }

        if (composer.DisposableSingletonsCount > 0)
        {
            // DisposeIndex field
            code.AppendLine($"private int {Variable.DisposeIndexFieldName};");
            membersCounter++;
        }
            
        // Disposables field
        code.AppendLine($"private {CodeConstants.DisposableTypeName}[] {Variable.DisposablesFieldName};");
        membersCounter++;

        // Singleton fields
        foreach (var singletonField in composer.Singletons)
        {
            cancellationToken.ThrowIfCancellationRequested();
            code.AppendLine($"private {singletonField.Node.Type} {singletonField.Name};");
            membersCounter++;

            if (!singletonField.Node.Type.IsValueType)
            {
                continue;
            }

            code.AppendLine($"private bool {singletonField.Name}Created;");
            membersCounter++;
        }

        return composer with { MembersCount = membersCounter };
    }
}