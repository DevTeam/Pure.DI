namespace Pure.DI.Core.CSharp;

internal class ResolverClassBuilder: IBuilder<CompositionCode, CompositionCode>
{
    internal static readonly string ResolverClassName = $"Resolver{Variable.Postfix}";
    internal static readonly string ResolveMethodName = "Resolve";
    internal static readonly string ResolveByTagMethodName = "ResolveByTag";
    
    public CompositionCode Build(CompositionCode composition, CancellationToken cancellationToken)
    {
        var actualRoots = composition.Roots.Where(i => !i.Injection.Type.IsRefLikeType).ToArray();
        if (!actualRoots.Any())
        {
            return composition;
        }
        
        var code = composition.Code;
        if (composition.MembersCount > 0)
        {
            code.AppendLine();
        }
        
        code.AppendLine("#pragma warning disable CS0649");
        code.AppendLine($"private class {ResolverClassName}<T>");
        code.AppendLine("{");
        using (code.Indent())
        {
            code.AppendLine($"public static Func<{composition.ClassName}, T> {ResolveMethodName} = composition => throw new System.InvalidOperationException($\"Cannot resolve composition root of type {{typeof(T)}}.\");");
            code.AppendLine($"public static Func<{composition.ClassName}, object, T> {ResolveByTagMethodName} = (composition, tag) => throw new System.InvalidOperationException($\"Cannot resolve composition root \\\"{{tag}}\\\" of type {{typeof(T)}}.\");");
        }
        code.AppendLine("}");
        code.AppendLine("#pragma warning restore CS0649");
        return composition with { MembersCount = composition.MembersCount + 1 };
    }
}