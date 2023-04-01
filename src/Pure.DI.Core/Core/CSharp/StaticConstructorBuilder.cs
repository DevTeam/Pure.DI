namespace Pure.DI.Core.CSharp;

internal class StaticConstructorBuilder: IBuilder<CompositionCode, CompositionCode>
{
    public CompositionCode Build(CompositionCode composition, CancellationToken cancellationToken)
    {
        var actualRoots = composition.Roots.GetActualRoots().ToArray();
        if (!actualRoots.Any())
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
            var groups = actualRoots.GroupBy(i => i.Injection.Type, SymbolEqualityComparer.Default).ToArray();
            foreach (var roots in groups)
            {
                var className = ResolverClassesBuilder.GetResolveClassName(roots.Key);
                code.AppendLine($"{className} val{className} = new {className}();");
                code.AppendLine($"{ResolverClassesBuilder.ResolverClassName}<{roots.Key}>.{ResolverClassesBuilder.ResolverPropertyName} = val{className};");
            }
            
            var divisor = Buckets<object, object>.GetDivisor((uint)groups.Length);
            var pairs = $"System.Type, {ResolverClassesBuilder.ResolverInterfaceName}<{composition.Name.ClassName}>";
            var bucketsTypeName = $"{CodeConstants.ApiNamespace}Buckets<{pairs}>";
            var pairTypeName = $"{CodeConstants.ApiNamespace}Pair<{pairs}>";
            code.AppendLine($"{ResolversFieldsBuilder.BucketsFieldName} = {bucketsTypeName}.{nameof(Buckets<object, object>.Create)}(");
            using (code.Indent())
            {
                code.AppendLine($"{divisor},");
                code.AppendLine($"new {pairTypeName}[{groups.Length}]");
                code.AppendLine("{");
                using (code.Indent())
                {
                    var isFirst = true;
                    foreach (var roots in groups)
                    {
                        var className = ResolverClassesBuilder.GetResolveClassName(roots.Key);
                        code.AppendLine($"{(isFirst ? ' ' : ',')}new {pairTypeName}(typeof({roots.Key}), val{className})");
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