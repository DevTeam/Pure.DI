// ReSharper disable ConvertIfStatementToConditionalTernaryExpression
// ReSharper disable InvertIf
namespace Pure.DI.Core.CSharp;

internal class ResolverClassesBuilder: IBuilder<CompositionCode, CompositionCode>
{
    private readonly IBuilder<ImmutableArray<Root>, IEnumerable<ResolverInfo>> _resolversBuilder;
    internal const string ResolverInterfaceName = $"{Constant.ApiNamespace}{nameof(IResolver<object, object>)}";
    internal const string ResolverPropertyName = "Value";
    internal const string ResolveMethodName = nameof(IResolver<object, object>.Resolve);
    internal const string ResolveByTagMethodName = nameof(IResolver<object, object>.ResolveByTag);

    public ResolverClassesBuilder(
        IBuilder<ImmutableArray<Root>, IEnumerable<ResolverInfo>> resolversBuilder)
    {
        _resolversBuilder = resolversBuilder;
    }

    public CompositionCode Build(CompositionCode composition, CancellationToken cancellationToken)
    {
        if (composition.Source.Source.Hints.GetHint(Hint.Resolve, SettingState.On) != SettingState.On)
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
            code.AppendLine($"public static {ResolverInterfaceName}<{composition.Name.ClassName}, T> {ResolverPropertyName};");
        }
        code.AppendLine("}");
        
        var resolvers = _resolversBuilder.Build(composition.Roots, cancellationToken).ToArray();
        if (resolvers.Any())
        {
            foreach (var resolver in resolvers)
            {
                var resolverClassName = resolver.ClassName;
                code.AppendLine();
                var interfaceName = $"{ResolverInterfaceName}<{composition.Name.ClassName}, {resolver.Type}>";
                var interfaces = new List<string> { interfaceName };
                var objectInterfaceName = "";
                if (resolver.Type.IsValueType)
                {
                    objectInterfaceName = $"{ResolverInterfaceName}<{composition.Name.ClassName}, object>";
                    interfaces.Add(objectInterfaceName);
                }
                
                code.AppendLine($"private sealed class {resolverClassName}: {string.Join(", ", interfaces)}");
                code.AppendLine("{");
                using (code.Indent())
                {
                    ImplementInterface(composition, resolver, code);

                    if (!string.IsNullOrWhiteSpace(objectInterfaceName))
                    {
                        code.AppendLine($"object {objectInterfaceName}.{ResolveMethodName}({composition.Name.ClassName} composition)");
                        code.AppendLine("{");
                        using (code.Indent())
                        {
                            code.AppendLine($"return {ResolveMethodName}(composition);");
                        }
                        code.AppendLine("}");
                        
                        code.AppendLine();
                        code.AppendLine($"object {objectInterfaceName}.{ResolveByTagMethodName}({composition.Name.ClassName} composition, object tag)");
                        code.AppendLine("{");
                        using (code.Indent())
                        {
                            code.AppendLine($"return {ResolveByTagMethodName}(composition, tag);");
                        }
                        code.AppendLine("}");
                    }
                }
                code.AppendLine("}");   
            }
        }
        
        code.AppendLine("#pragma warning restore CS0649");
        code.AppendLine("#endregion");
        return composition with { MembersCount = composition.MembersCount + 1 };
    }

    private static void ImplementInterface(CompositionCode composition, ResolverInfo resolver, LinesBuilder code)
    {
        var defaultRoot = resolver.Roots.SingleOrDefault(i => i.Injection.Tag is null);

        code.AppendLine(Constant.MethodImplOptions);
        code.AppendLine($"public {resolver.Type} {ResolveMethodName}({composition.Name.ClassName} composition)");
        code.AppendLine("{");
        using (code.Indent())
        {
            if (defaultRoot is not null)
            {
                code.AppendLine($"return composition.{defaultRoot.PropertyName};");
            }
            else
            {
                code.AppendLine($"throw new System.InvalidOperationException($\"{Constant.CannotResolve} of type {resolver.Type}.\");");
            }
        }

        code.AppendLine("}");

        code.AppendLine();

        code.AppendLine(Constant.MethodImplOptions);
        code.AppendLine($"public {resolver.Type} {ResolveByTagMethodName}({composition.Name.ClassName} composition, object tag)");
        code.AppendLine("{");
        using (code.Indent())
        {
            var taggedRoots = resolver.Roots.Where(i => i.Injection.Tag is not null).ToArray();
            foreach (var taggedRoot in taggedRoots)
            {
                code.AppendLine($"if (Equals(tag, {taggedRoot.Injection.Tag.ValueToString()})) return composition.{taggedRoot.PropertyName};");
            }

            if (defaultRoot is not null)
            {
                code.AppendLine($"if (Equals(tag, null)) return composition.{defaultRoot.PropertyName};");
            }

            code.AppendLine($"throw new System.InvalidOperationException($\"{Constant.CannotResolve} \\\"{{tag}}\\\" of type {resolver.Type}.\");");
        }

        code.AppendLine("}");
    }
}