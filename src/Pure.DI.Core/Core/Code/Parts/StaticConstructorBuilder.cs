// ReSharper disable ClassNeverInstantiated.Global

namespace Pure.DI.Core.Code.Parts;

using static LinesBuilderExtensions;

sealed class StaticConstructorBuilder(
    ITypeResolver typeResolver,
    IBuilder<RootContext, IEnumerable<ResolverInfo>> resolversBuilder)
    : IClassPartBuilder
{
    public ClassPart Part => ClassPart.StaticConstructor;

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
            code.AppendLine($"private static partial void {Names.OnNewRootMethodName}<TContract, T>({Names.IResolverTypeName}<{composition.Source.Source.Name.ClassName}, TContract> resolver, string name, object? tag, {Names.ApiNamespace}{nameof(Lifetime)} lifetime);");
            code.AppendLine();
            membersCounter++;
        }

        var resolvers = resolversBuilder.Build(new RootContext(composition.Source.Source, composition.Roots)).ToList();
        if (resolvers.Count == 0)
        {
            return composition;
        }

        code.AppendLine($"static {composition.Source.Source.Name.ClassName}()");
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
                        code.AppendLine($"{Names.OnNewRootMethodName}<{typeResolver.Resolve(composition.Source.Source, root.Injection.Type)}, {typeResolver.Resolve(composition.Source.Source, root.Node.Type)}>(val{className}, \"{root.DisplayName}\", {root.Injection.Tag.ValueToString()}, {root.Node.Lifetime.ValueToString()});");
                    }
                }

                code.AppendLine($"{Names.ResolverClassName}<{typeResolver.Resolve(composition.Source.Source, resolver.Type)}>.{Names.ResolverPropertyName} = val{className};");
            }

            var divisor = Buckets<object, object>.GetDivisor((uint)resolvers.Count);
            var pairs = $"{Names.SystemNamespace}Type, {Names.IResolverTypeName}<{composition.Source.Source.Name.ClassName}, object>";
            var bucketsTypeName = $"{Names.ApiNamespace}Buckets<{pairs}>";
            var pairTypeName = $"{Names.ApiNamespace}Pair<{pairs}>";
            code.AppendLine($"{Names.BucketsFieldName} = {bucketsTypeName}.{nameof(Buckets<object, object>.Create)}(");
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
                        code.AppendLine($"{(isFirst ? " " : ",")}new {pairTypeName}(typeof({resolver.Type}), val{className})");
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