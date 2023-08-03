// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core.Code;

internal sealed class StaticConstructorBuilder: IBuilder<CompositionCode, CompositionCode>
{
    private readonly IBuilder<ImmutableArray<Root>, IEnumerable<ResolverInfo>> _resolversBuilder;

    public StaticConstructorBuilder(
        IBuilder<ImmutableArray<Root>, IEnumerable<ResolverInfo>> resolversBuilder)
    {
        _resolversBuilder = resolversBuilder;
    }

    public CompositionCode Build(CompositionCode composition)
    {
        if (composition.Source.Source.Hints.GetHint(Hint.Resolve, SettingState.On) != SettingState.On)
        {
            return composition;
        }

        var resolvers = _resolversBuilder.Build(composition.Roots).ToArray();
        if (!resolvers.Any())
        {
            return composition;
        }

        var code = composition.Code;
        if (composition.MembersCount > 0)
        {
            code.AppendLine();
        }

        var hasOnNewRoot = composition.Source.Source.Hints.GetHint(Hint.OnNewRoot) == SettingState.On;
        
        var membersCounter = 0;
        code.AppendLine($"static {composition.Name.ClassName}()");
        code.AppendLine("{");
        using (code.Indent())
        {
            foreach (var resolver in resolvers)
            {
                var className = resolver.ClassName;
                code.AppendLine($"{className} val{className} = new {className}();");
                if (hasOnNewRoot)
                {
                    foreach (var root in resolver.Roots)
                    {
                        code.AppendLine($"{Constant.OnNewRootMethodName}<{root.Injection.Type}, {root.Node.Type}>(val{className}, \"{root.PropertyName}\", {root.Injection.Tag.ValueToString()}, {root.Node.Lifetime.ValueToString()});");
                    }
                }
                
                code.AppendLine($"{ResolverInfo.ResolverClassName}<{resolver.Type}>.{ResolverClassesBuilder.ResolverPropertyName} = val{className};");
            }
            
            var divisor = Buckets<object, object>.GetDivisor((uint)resolvers.Length);
            var pairs = $"{Constant.SystemNamespace}Type, {ResolverClassesBuilder.ResolverInterfaceName}<{composition.Name.ClassName}, object>";
            var bucketsTypeName = $"{Constant.ApiNamespace}Buckets<{pairs}>";
            var pairTypeName = $"{Constant.ApiNamespace}Pair<{pairs}>";
            code.AppendLine($"{ResolversFieldsBuilder.BucketsFieldName} = {bucketsTypeName}.{nameof(Buckets<object, object>.Create)}(");
            using (code.Indent())
            {
                code.AppendLine($"{divisor.ToString()},");
                code.AppendLine($"out {ResolversFieldsBuilder.BucketSizeFieldName},");
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
        
        // ReSharper disable once InvertIf
        if (hasOnNewRoot)
        {
            code.AppendLine();
            code.AppendLine(Constant.MethodImplOptions);
            code.AppendLine($"private static partial void {Constant.OnNewRootMethodName}<TContract, T>({ResolverClassesBuilder.ResolverInterfaceName}<{composition.Name.ClassName}, TContract> resolver, string name, object? tag, {Constant.ApiNamespace}{nameof(Lifetime)} lifetime);");
            membersCounter++;
        }
        
        return composition with { MembersCount = composition.MembersCount + membersCounter };
    }
}