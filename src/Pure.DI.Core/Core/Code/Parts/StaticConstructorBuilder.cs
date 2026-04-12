// ReSharper disable ClassNeverInstantiated.Global

namespace Pure.DI.Core.Code.Parts;

using static LinesExtensions;

sealed class StaticConstructorBuilder(
    ITypeResolver typeResolver,
    IBuilder<RootsContext, IEnumerable<ResolverInfo>> resolversBuilder,
    ICodeNameProvider codeNameProvider)
    : IClassPartBuilder
{
    public ClassPart Part => ClassPart.StaticConstructor;

    public CompositionCode Build(CompositionCode composition)
    {
        var hints = composition.Hints;
        if (!hints.IsResolveEnabled)
        {
            return composition;
        }

        var code = composition.Code;
        var membersCounter = 0;
        var hasOnNewRoot = hints.IsOnNewRootEnabled;
        // ReSharper disable once InvertIf
        if (hasOnNewRoot && hints.IsOnNewRootPartial)
        {
            code.AppendLine($"private static partial void {Names.OnNewRootMethodName}<TContract, T>({Names.IResolverTypeName}<{composition.Name.ClassName}, TContract> resolver, string name, object? tag, {Names.ApiNamespace}{nameof(Lifetime)} lifetime);");
            code.AppendLine();
            membersCounter++;
        }

        var resolvers = resolversBuilder.Build(new RootsContext(composition.Setup, composition.PublicRoots)).ToList();
        if (resolvers.Count == 0)
        {
            return composition;
        }

        var ctorName = codeNameProvider.GetConstructorName(composition.Name.ClassName);
        code.AppendLine($"static {ctorName}()");
        using (code.CreateBlock())
        {
            foreach (var resolver in resolvers)
            {
                var className = resolver.ClassName;
                code.AppendLine($"var val{className} = new {className}();");
                if (hasOnNewRoot)
                {
                    foreach (var root in resolver.Roots)
                    {
                        code.AppendLine($"{Names.OnNewRootMethodName}<{typeResolver.Resolve(composition.Setup, root.Injection.Type)}, {typeResolver.Resolve(composition.Source.Source, root.Node.Type)}>(val{className}, \"{root.DisplayName}\", {root.Injection.Tag.ValueToString()}, {root.Node.Lifetime.ValueToString()});");
                    }
                }

                code.AppendLine($"{Names.ResolverClassName}<{typeResolver.Resolve(composition.Setup, resolver.Type)}>.{Names.ResolverPropertyName} = val{className};");
            }

            var divisor = Buckets<object>.GetDivisor((uint)resolvers.Count);
            var pairs = $"{Names.IResolverTypeName}<{composition.Name.ClassName}, object>";
            var bucketsTypeName = $"{Names.ApiNamespace}Buckets<{pairs}>";
            var pairTypeName = $"{Names.ApiNamespace}Pair<{pairs}>";
            code.AppendLine($"{Names.BucketsFieldName} = {bucketsTypeName}.{nameof(Buckets<>.Create)}(");
            using (code.Indent())
            {
                code.AppendLine($"{divisor.ToString()},");
                code.AppendLine($"out {Names.BucketSizeFieldName},");
                code.AppendLine($"new {pairTypeName}[{resolvers.Count.ToString()}]");
                code.AppendLine(BlockStart);
                using (code.Indent())
                {
                    var isFirst = true;
                    foreach (var resolver in resolvers)
                    {
                        var className = resolver.ClassName;
                        var resolverType = typeResolver.Resolve(composition.Setup, resolver.Type);
                        code.AppendLine($"{(isFirst ? " " : ",")}new {pairTypeName}(typeof({resolverType}), val{className})");
                        isFirst = false;
                    }
                }

                code.AppendLine("});");
            }
        }

        membersCounter++;

        return composition with { MembersCount = composition.MembersCount + membersCounter };
    }

}
