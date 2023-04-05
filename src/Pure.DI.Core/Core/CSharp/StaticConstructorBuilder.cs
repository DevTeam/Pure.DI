namespace Pure.DI.Core.CSharp;

internal class StaticConstructorBuilder: IBuilder<CompositionCode, CompositionCode>
{
    private readonly IBuilder<ImmutableArray<Root>, IEnumerable<ResolverInfo>> _resolversBuilder;

    public StaticConstructorBuilder(
        IBuilder<ImmutableArray<Root>, IEnumerable<ResolverInfo>> resolversBuilder)
    {
        _resolversBuilder = resolversBuilder;
    }

    public CompositionCode Build(CompositionCode composition, CancellationToken cancellationToken)
    {
        if (composition.Source.Source.Settings.GetState(Setting.Resolve, SettingState.On) != SettingState.On)
        {
            return composition;
        }

        var resolvers = _resolversBuilder.Build(composition.Roots, cancellationToken).ToArray();
        if (!resolvers.Any())
        {
            return composition;
        }

        var code = composition.Code;
        if (composition.MembersCount > 0)
        {
            code.AppendLine();
        }

        code.AppendLine($"static {composition.Name.ClassName}()");
        code.AppendLine("{");
        using (code.Indent())
        {
            foreach (var resolver in resolvers)
            {
                var className = resolver.ClassName;
                code.AppendLine($"{className} val{className} = new {className}();");
                code.AppendLine($"{ResolverInfo.ResolverClassName}<{resolver.Type}>.{ResolverClassesBuilder.ResolverPropertyName} = val{className};");
            }
            
            var divisor = Buckets<object, object>.GetDivisor((uint)resolvers.Length);
            var pairs = $"System.Type, {ResolverClassesBuilder.ResolverInterfaceName}<{composition.Name.ClassName}>";
            var bucketsTypeName = $"{Constant.ApiNamespace}Buckets<{pairs}>";
            var pairTypeName = $"{Constant.ApiNamespace}Pair<{pairs}>";
            code.AppendLine($"{ResolversFieldsBuilder.BucketsFieldName} = {bucketsTypeName}.{nameof(Buckets<object, object>.Create)}(");
            using (code.Indent())
            {
                code.AppendLine($"{divisor.ToString()},");
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
        return composition with { MembersCount = composition.MembersCount + 1 };
    }
}