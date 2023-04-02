// ReSharper disable ConvertIfStatementToConditionalTernaryExpression
namespace Pure.DI.Core.CSharp;

internal class ResolverClassesBuilder: IBuilder<CompositionCode, CompositionCode>
{
    internal static readonly string ResolverInterfaceName = $"{CodeConstants.ApiNamespace}{nameof(IResolver<object, object>)}";
    internal static readonly string ResolverClassName = $"Resolver{Variable.Postfix}";
    internal static readonly string ResolverPropertyName = "Value";
    internal static readonly string ResolveMethodName = nameof(IResolver<object, object>.Resolve);
    internal static readonly string ResolveByTagMethodName = nameof(IResolver<object, object>.ResolveByTag);

    [SuppressMessage("MicrosoftCodeAnalysisCorrectness", "RS1024:Symbols should be compared for equality")]
    public static string GetResolveClassName(ISymbol? type) => 
        $"{ResolverClassName}{(uint)(type?.GetHashCode() ?? 0)}";
    
    public CompositionCode Build(CompositionCode composition, CancellationToken cancellationToken)
    {
        var code = composition.Code;
        if (composition.MembersCount > 0)
        {
            code.AppendLine();
        }
        
        code.AppendLine("#region Resolvers");
        code.AppendLine("#pragma warning disable CS0649");
        code.AppendLine($"private class {ResolverClassName}<T>");
        code.AppendLine("{");
        using (code.Indent())
        {
            code.AppendLine($"public static {ResolverInterfaceName}<{composition.Name.ClassName}, T>? {ResolverPropertyName};");
        }
        code.AppendLine("}");
        
        var actualRoots = composition.Roots.GetActualRoots().ToArray();
        if (actualRoots.Any())
        {
            var groups = actualRoots.GroupBy(i => i.Injection.Type, SymbolEqualityComparer.Default);
            foreach (var roots in groups)
            {
                var resolverClassName = GetResolveClassName(roots.Key);
                code.AppendLine();
                code.AppendLine($"private class {resolverClassName}: {ResolverInterfaceName}<{composition.Name.ClassName}, {roots.Key}>");
                code.AppendLine("{");
                using (code.Indent())
                {
                    GenerateResolverMethods(composition, roots, roots.Key?.ToString() ?? "", "", "", code);
                    code.AppendLine();
                    GenerateResolverMethods(composition, roots, "object", "Object", "(object)", code);
                }
                code.AppendLine("}");   
            }
        }
        
        code.AppendLine("#pragma warning restore CS0649");
        code.AppendLine("#endregion");
        return composition with { MembersCount = composition.MembersCount + 1 };
    }

    private static void GenerateResolverMethods(
        CompositionCode composition,
        IGrouping<ISymbol?, Root> roots,
        string returnType,
        string methodPrefix,
        string cast,
        LinesBuilder code)
    {
        var defaultRoot = roots.SingleOrDefault(i => i.Injection.Tag is not { });
        code.AppendLine($"public {returnType} {methodPrefix}{ResolveMethodName}({composition.Name.ClassName} composition)");
        code.AppendLine("{");
        using (code.Indent())
        {
            if (defaultRoot is { })
            {
                code.AppendLine($"return {cast}composition.{defaultRoot.PropertyName};");
            }
            else
            {
                code.AppendLine($"throw new System.InvalidOperationException($\"{CodeConstants.CannotResolve} of type {roots.Key}.\");");
            }
        }

        code.AppendLine("}");

        code.AppendLine();

        code.AppendLine($"public {returnType} {methodPrefix}{ResolveByTagMethodName}({composition.Name.ClassName} composition, object tag)");
        code.AppendLine("{");
        using (code.Indent())
        {
            var taggedRoots = roots.Where(i => i.Injection.Tag is { }).ToArray();
            foreach (var taggedRoot in taggedRoots)
            {
                code.AppendLine($"if (Equals(tag, {taggedRoot.Injection.Tag.TagToString()})) return {cast}composition.{taggedRoot.PropertyName};");
            }

            if (defaultRoot is { })
            {
                code.AppendLine($"if (Equals(tag, null)) return {cast}composition.{defaultRoot.PropertyName};");
            }

            code.AppendLine($"throw new System.InvalidOperationException($\"{CodeConstants.CannotResolve} \\\"{{tag}}\\\" of type {roots.Key}.\");");
        }

        code.AppendLine("}");
    }
}