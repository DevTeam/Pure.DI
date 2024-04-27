// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core.Code;

internal sealed class StaticConstructorBuilder(
    ITypeResolver typeResolver,
    IBuilder<ImmutableArray<Root>,
    IEnumerable<ResolverInfo>> resolversBuilder)
    : IBuilder<CompositionCode, CompositionCode>
{
    public CompositionCode Build(CompositionCode composition)
    {
        if (!composition.Source.Source.Hints.IsResolveEnabled)
        {
            return composition;
        }
        
        var code = composition.Code;
        var membersCounter = 0;
        var hasOnNewRoot = composition.Source.Source.Hints.IsOnNewRootEnabled;
        // ReSharper disable once InvertIf
        if (hasOnNewRoot && composition.Source.Source.Hints.IsOnNewRootPartial)
        {
            code.AppendLine($"private static partial void {Names.OnNewRootMethodName}<TContract, T>({Names.ResolverInterfaceName}<{composition.Source.Source.Name.ClassName}, TContract> resolver, string name, object? tag, {Names.ApiNamespace}{nameof(Lifetime)} lifetime);");
            code.AppendLine();
            membersCounter++;
        }

        var resolvers = resolversBuilder.Build(composition.Roots).ToArray();
        if (!resolvers.Any())
        {
            return composition;
        }
        
        code.AppendLine($"static {composition.Source.Source.Name.ClassName}()");
        code.AppendLine("{");
        using (code.Indent())
        {
            foreach (var resolver in resolvers)
            {
                var className = resolver.ClassName;
                code.AppendLine($"var val{className} = new {className}();");
                if (hasOnNewRoot)
                {
                    foreach (var root in resolver.Roots)
                    {
                        code.AppendLine($"{Names.OnNewRootMethodName}<{typeResolver.Resolve(root.Injection.Type)}, {typeResolver.Resolve(root.Node.Type)}>(val{className}, \"{root.DisplayName}\", {root.Injection.Tag.ValueToString()}, {root.Node.Lifetime.ValueToString()});");
                    }
                }
                
                code.AppendLine($"{Names.ResolverClassName}<{typeResolver.Resolve(resolver.Type)}>.{Names.ResolverPropertyName} = val{className};");
            }
            
            var divisor = Buckets<object, object>.GetDivisor((uint)resolvers.Length);
            var pairs = $"{Names.SystemNamespace}Type, {Names.ResolverInterfaceName}<{composition.Source.Source.Name.ClassName}, object>";
            var bucketsTypeName = $"{Names.ApiNamespace}Buckets<{pairs}>";
            var pairTypeName = $"{Names.ApiNamespace}Pair<{pairs}>";
            code.AppendLine($"{Names.BucketsFieldName} = {bucketsTypeName}.{nameof(Buckets<object, object>.Create)}(");
            using (code.Indent())
            {
                code.AppendLine($"{divisor.ToString()},");
                code.AppendLine($"out {Names.BucketSizeFieldName},");
                code.AppendLine($"new {pairTypeName}[{resolvers.Length.ToString()}]");
                code.AppendLine("{");
                using (code.Indent())
                {
                    var isFirst = true;
                    foreach (var resolver in resolvers)
                    {
                        var className = resolver.ClassName;
                        code.AppendLine($"{(isFirst ? " " : ",")}new {pairTypeName}(typeof({resolver.Type}), val{className})");
                        isFirst = false;
                    }
                }

                code.AppendLine("});");
            }
        }
        
        code.AppendLine("}");
        membersCounter++;
        
        return composition with { MembersCount = composition.MembersCount + membersCounter };
    }
}