// ReSharper disable ConvertIfStatementToConditionalTernaryExpression
// ReSharper disable InvertIf
namespace Pure.DI.Core.CSharp;

internal class ResolverClassesBuilder: IBuilder<CompositionCode, CompositionCode>
{
    private readonly IBuilder<IEnumerable<Root>, IEnumerable<ResolverInfo>> _resolversBuilder;
    internal static readonly string ResolverInterfaceName = $"{Constant.ApiNamespace}{nameof(IResolver<object, object>)}";
    internal static readonly string ResolverPropertyName = "Value";
    internal static readonly string ResolveMethodName = nameof(IResolver<object, object>.Resolve);
    internal static readonly string ResolveByTagMethodName = nameof(IResolver<object, object>.ResolveByTag);
    
    public ResolverClassesBuilder(
        IBuilder<IEnumerable<Root>, IEnumerable<ResolverInfo>> resolversBuilder)
    {
        _resolversBuilder = resolversBuilder;
    }

    public CompositionCode Build(CompositionCode composition, CancellationToken cancellationToken)
    {
        if (composition.Source.Source.Settings.GetState(Setting.Resolve, SettingState.On) != SettingState.On)
        {
            return composition;
        }
        
        var code = composition.Code;
        if (composition.MembersCount > 0)
        {
            code.AppendLine();
        }
        
        code.AppendLine("#region Resolvers");
        code.AppendLine("#pragma warning disable CS0649");
        code.AppendLine($"private class {ResolverInfo.ResolverClassName}<T>");
        code.AppendLine("{");
        using (code.Indent())
        {
            code.AppendLine($"public static {ResolverInterfaceName}<{composition.Name.ClassName}, T>? {ResolverPropertyName};");
        }
        code.AppendLine("}");
        
        var resolvers = _resolversBuilder.Build(composition.Roots, cancellationToken).ToArray();
        if (resolvers.Any())
        {
            foreach (var resolver in resolvers)
            {
                var resolverClassName = resolver.ClassName;
                code.AppendLine();
                code.AppendLine($"private sealed class {resolverClassName}: {ResolverInterfaceName}<{composition.Name.ClassName}, {resolver.Type}>");
                code.AppendLine("{");
                using (code.Indent())
                {
                    GenerateResolverMethods(composition, resolver, resolver.Type.ToString() ?? "", "", "", code);
                    code.AppendLine();
                    GenerateResolverMethods(composition, resolver, "object", "Object", "(object)", code);
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
        ResolverInfo resolver,
        string returnType,
        string methodPrefix,
        string cast,
        LinesBuilder code)
    {
        var defaultRoot = resolver.Roots.SingleOrDefault(i => i.Injection.Tag is not { });
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
                code.AppendLine($"throw new System.InvalidOperationException($\"{Constant.CannotResolve} of type {resolver.Type}.\");");
            }
        }

        code.AppendLine("}");

        code.AppendLine();

        code.AppendLine($"public {returnType} {methodPrefix}{ResolveByTagMethodName}({composition.Name.ClassName} composition, object tag)");
        code.AppendLine("{");
        using (code.Indent())
        {
            var taggedRoots = resolver.Roots.Where(i => i.Injection.Tag is { }).ToArray();
            foreach (var taggedRoot in taggedRoots)
            {
                code.AppendLine($"if (Equals(tag, {taggedRoot.Injection.Tag.TagToString()})) return {cast}composition.{taggedRoot.PropertyName};");
            }

            if (defaultRoot is { })
            {
                code.AppendLine($"if (Equals(tag, null)) return {cast}composition.{defaultRoot.PropertyName};");
            }

            code.AppendLine($"throw new System.InvalidOperationException($\"{Constant.CannotResolve} \\\"{{tag}}\\\" of type {resolver.Type}.\");");
        }

        code.AppendLine("}");
    }
}