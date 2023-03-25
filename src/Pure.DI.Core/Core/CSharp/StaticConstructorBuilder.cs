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

        code.AppendLine($"static {composition.ClassName}()");
        code.AppendLine("{");
        using (code.Indent())
        {
            var groups = actualRoots.GroupBy(i => i.Injection.Type, SymbolEqualityComparer.Default);
            foreach (var roots in groups)
            {
                var defaultRoot = roots.SingleOrDefault(i => i.Injection.Tag is not { });
                if (defaultRoot is { })
                {
                    code.AppendLine($"{ResolverClassBuilder.ResolverClassName}<{roots.Key}>.{ResolverClassBuilder.ResolveMethodName} = composition => composition.{defaultRoot.PropertyName};");
                }
                
                var taggedRoots = roots.Where(i => i.Injection.Tag is { }).ToArray();
                if (taggedRoots.Any())
                {
                    code.AppendLine($"{ResolverClassBuilder.ResolverClassName}<{roots.Key}>.{ResolverClassBuilder.ResolveByTagMethodName} = (composition, tag) => ");
                    code.AppendLine("{");
                    using (code.Indent())
                    {
                        foreach (var taggedRoot in taggedRoots)
                        {
                            code.AppendLine($"if (Equals(tag, {taggedRoot.Injection.Tag.TagToString()})) return composition.{taggedRoot.PropertyName};");
                        }

                        code.AppendLine($"throw new System.InvalidOperationException($\"{CodeExtensions.CannotResolve} \\\"{{tag}}\\\" of type {roots.Key}.\");");
                    }

                    code.AppendLine("};");
                }
            }
        }
        
        code.AppendLine("}");
        return composition with { MembersCount = composition.MembersCount + 1 };
    }
}