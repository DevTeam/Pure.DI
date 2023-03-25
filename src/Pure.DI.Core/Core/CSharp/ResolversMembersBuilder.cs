namespace Pure.DI.Core.CSharp;

internal class ResolversMembersBuilder: IBuilder<CompositionCode, CompositionCode>
{
    internal static readonly string ResolversInitializerMethodName = $"InitializeResolvers{Variable.Postfix}";
    internal static readonly string ResolverMethodName = "Resolve";
    
    public CompositionCode Build(CompositionCode composition, CancellationToken cancellationToken)
    {
        var code = composition.Code;
        var membersCounter = composition.MembersCount;
        if (composition.MembersCount > 0)
        {
            code.AppendLine();
        }
        
        code.AppendLine(Syntax.MethodImplOptions);
        code.AppendLine($"public T {ResolverMethodName}<T>()");
        code.AppendLine("{");
        using (code.Indent())
        {
            code.AppendLine($"return {ResolverClassBuilder.ResolverClassName}<T>.{ResolverClassBuilder.ResolveMethodName}(this);");
        }
        code.AppendLine("}");
        
        code.AppendLine();
        membersCounter++;
        
        code.AppendLine(Syntax.MethodImplOptions);
        code.AppendLine($"public T {ResolverMethodName}<T>(object tag)");
        code.AppendLine("{");
        using (code.Indent())
        {
            code.AppendLine($"return {ResolverClassBuilder.ResolverClassName}<T>.{ResolverClassBuilder.ResolveByTagMethodName}(this, tag);");
        }
        code.AppendLine("}");
        
        return composition with { MembersCount = membersCounter };
    }
}