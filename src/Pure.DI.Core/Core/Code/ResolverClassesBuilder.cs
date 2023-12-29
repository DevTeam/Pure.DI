// ReSharper disable ConvertIfStatementToConditionalTernaryExpression
// ReSharper disable InvertIf
// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core.Code;

internal sealed class ResolverClassesBuilder(IBuilder<ImmutableArray<Root>, IEnumerable<ResolverInfo>> resolversBuilder)
    : IBuilder<CompositionCode, CompositionCode>
{
    public CompositionCode Build(CompositionCode composition)
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
        code.AppendLine($"private sealed class {Names.ResolverClassName}<T>: {Names.ResolverInterfaceName}<{composition.Source.Source.Name.ClassName}, T>");
        code.AppendLine("{");
        using (code.Indent())
        {
            code.AppendLine($"public static {Names.ResolverInterfaceName}<{composition.Source.Source.Name.ClassName}, T> {Names.ResolverPropertyName} = new {Names.ResolverClassName}<T>();");

            code.AppendLine();
            code.AppendLine($"public T {Names.ResolveMethodName}({composition.Source.Source.Name.ClassName} composite)");
            code.AppendLine("{");
            using (code.Indent())
            {
                code.AppendLine($"throw new {Names.SystemNamespace}InvalidOperationException($\"{Names.CannotResolve} of type {{typeof(T)}}.\");");
            }
            
            code.AppendLine("}");
            
            code.AppendLine();
            code.AppendLine($"public T {Names.ResolveByTagMethodName}({composition.Source.Source.Name.ClassName} composite, object tag)");
            code.AppendLine("{");
            using (code.Indent())
            {
                code.AppendLine($"throw new {Names.SystemNamespace}InvalidOperationException($\"{Names.CannotResolve} \\\"{{tag}}\\\" of type {{typeof(T)}}.\");");
            }
            
            code.AppendLine("}");
        }
        code.AppendLine("}");
        
        var resolvers = resolversBuilder.Build(composition.Roots).ToArray();
        if (resolvers.Any())
        {
            foreach (var resolver in resolvers)
            {
                var resolverClassName = resolver.ClassName;
                code.AppendLine();
                var interfaceName = $"{Names.ResolverInterfaceName}<{composition.Source.Source.Name.ClassName}, {resolver.Type}>";
                var interfaces = new List<string> { interfaceName };
                var objectInterfaceName = "";
                if (resolver.Type.IsValueType)
                {
                    objectInterfaceName = $"{Names.ResolverInterfaceName}<{composition.Source.Source.Name.ClassName}, object>";
                    interfaces.Add(objectInterfaceName);
                }
                
                code.AppendLine($"private sealed class {resolverClassName}: {string.Join(", ", interfaces)}");
                code.AppendLine("{");
                using (code.Indent())
                {
                    ImplementInterface(composition, resolver, code);

                    if (!string.IsNullOrWhiteSpace(objectInterfaceName))
                    {
                        code.AppendLine($"object {objectInterfaceName}.{Names.ResolveMethodName}({composition.Source.Source.Name.ClassName} composition)");
                        code.AppendLine("{");
                        using (code.Indent())
                        {
                            code.AppendLine($"return {Names.ResolveMethodName}(composition);");
                        }
                        code.AppendLine("}");
                        
                        code.AppendLine();
                        code.AppendLine($"object {objectInterfaceName}.{Names.ResolveByTagMethodName}({composition.Source.Source.Name.ClassName} composition, object tag)");
                        code.AppendLine("{");
                        using (code.Indent())
                        {
                            code.AppendLine($"return {Names.ResolveByTagMethodName}(composition, tag);");
                        }
                        code.AppendLine("}");
                    }
                }
                code.AppendLine("}");   
            }
        }
        
        code.AppendLine("#endregion");
        return composition with { MembersCount = composition.MembersCount + 1 };
    }

    private static void ImplementInterface(CompositionCode composition, ResolverInfo resolver, LinesBuilder code)
    {
        var defaultRoot = resolver.Roots.SingleOrDefault(i => i.Injection.Tag is null);

        code.AppendLine($"public {resolver.Type} {Names.ResolveMethodName}({composition.Source.Source.Name.ClassName} composition)");
        code.AppendLine("{");
        using (code.Indent())
        {
            if (defaultRoot is not null)
            {
                var isStatic = (defaultRoot.Kind & RootKinds.Static) == RootKinds.Static;
                var isMethod = !defaultRoot.Args.IsEmpty || (defaultRoot.Kind & RootKinds.Method) == RootKinds.Method;
                code.AppendLine($"return {(isStatic ? composition.Source.Source.Name.ClassName : "composition")}.{defaultRoot.PropertyName}{(isMethod ? "()": "")};");
            }
            else
            {
                code.AppendLine($"throw new {Names.SystemNamespace}InvalidOperationException($\"{Names.CannotResolve} of type {resolver.Type}.\");");
            }
        }

        code.AppendLine("}");

        code.AppendLine();

        code.AppendLine($"public {resolver.Type} {Names.ResolveByTagMethodName}({composition.Source.Source.Name.ClassName} composition, object tag)");
        code.AppendLine("{");
        using (code.Indent())
        {
            var taggedRoots = resolver.Roots.Where(i => i.Injection.Tag is not null).ToArray();
            foreach (var taggedRoot in taggedRoots)
            {
                code.AppendLine($"if (Equals(tag, {taggedRoot.Injection.Tag.ValueToString()})) return {GetRoot(composition, taggedRoot)};");
            }

            if (defaultRoot is not null)
            {
                code.AppendLine($"if (Equals(tag, null)) return {GetRoot(composition, defaultRoot)};");
            }

            code.AppendLine($"throw new {Names.SystemNamespace}InvalidOperationException($\"{Names.CannotResolve} \\\"{{tag}}\\\" of type {resolver.Type}.\");");
        }

        code.AppendLine("}");
    }

    private static string GetRoot(CompositionCode composition, Root root)
    {
        var target = (root.Kind & RootKinds.Static) == RootKinds.Static ? composition.Source.Source.Name.ClassName : "composition";
        var isMethod = !root.Args.IsEmpty || (root.Kind & RootKinds.Method) == RootKinds.Method;
        return $"{target}.{root.PropertyName}{(isMethod ? "()": "")}";
    }
}