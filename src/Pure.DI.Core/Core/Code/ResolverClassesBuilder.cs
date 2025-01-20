// ReSharper disable ConvertIfStatementToConditionalTernaryExpression
// ReSharper disable InvertIf
// ReSharper disable ClassNeverInstantiated.Global

namespace Pure.DI.Core.Code;

internal sealed class ResolverClassesBuilder(IBuilder<RootContext, IEnumerable<ResolverInfo>> resolversBuilder)
    : IBuilder<CompositionCode, CompositionCode>
{
    public CompositionCode Build(CompositionCode composition)
    {
        if (!composition.Source.Source.Hints.IsResolveEnabled)
        {
            return composition;
        }

        var membersCount = composition.MembersCount;
        var code = composition.Code;
        code.AppendLine("#region Resolvers");
        code.AppendLine($"private const string {Names.CannotResolveFieldName} = \"{Names.CannotResolveMessage} \";");
        membersCount++;
        code.AppendLine($"private const string {Names.OfTypeFieldName} = \"{Names.OfTypeMessage} \";");
        membersCount++;
        code.AppendLine();

        code.AppendLine($"private class {Names.ResolverClassName}<T>: {Names.IResolverTypeName}<{composition.Source.Source.Name.ClassName}, T>");
        code.AppendLine("{");
        using (code.Indent())
        {
            code.AppendLine($"public static {Names.IResolverTypeName}<{composition.Source.Source.Name.ClassName}, T> {Names.ResolverPropertyName} = new {Names.ResolverClassName}<T>();");
            code.AppendLine();
            code.AppendLine($"public virtual T {Names.ResolveMethodName}({composition.Source.Source.Name.ClassName} composite)");
            code.AppendLine("{");
            using (code.Indent())
            {
                code.AppendLine($"throw new {Names.SystemNamespace}InvalidOperationException($\"{{{Names.CannotResolveFieldName}}}{{{Names.OfTypeFieldName}}}{{typeof(T)}}.\");");
            }

            code.AppendLine("}");

            code.AppendLine();
            code.AppendLine($"public virtual T {Names.ResolveByTagMethodName}({composition.Source.Source.Name.ClassName} composite, object tag)");
            code.AppendLine("{");
            using (code.Indent())
            {
                code.AppendLine($"throw new {Names.SystemNamespace}InvalidOperationException($\"{{{Names.CannotResolveFieldName}}}\\\"{{tag}}\\\" {{{Names.OfTypeFieldName}}}{{typeof(T)}}.\");");
            }

            code.AppendLine("}");
        }

        code.AppendLine("}");
        membersCount++;

        foreach (var resolver in resolversBuilder.Build(new RootContext(composition.Source.Source, composition.Roots)))
        {
            var resolverClassName = resolver.ClassName;
            var baseTypeName = $"{Names.ResolverClassName}<{resolver.Type}>";
            var baseTypes = new List<string> { baseTypeName };
            var objectTypeName = "";
            if (resolver.Type.IsValueType)
            {
                objectTypeName = $"{Names.IResolverTypeName}<{composition.Source.Source.Name.ClassName}, object>";
                baseTypes.Add(objectTypeName);
            }

            code.AppendLine();
            code.AppendLine($"private sealed class {resolverClassName}: {string.Join(", ", baseTypes)}");
            code.AppendLine("{");
            using (code.Indent())
            {
                ImplementInterface(composition, resolver, code);

                if (!string.IsNullOrWhiteSpace(objectTypeName))
                {
                    code.AppendLine($"object {objectTypeName}.{Names.ResolveMethodName}({composition.Source.Source.Name.ClassName} composition)");
                    code.AppendLine("{");
                    using (code.Indent())
                    {
                        code.AppendLine($"return {Names.ResolveMethodName}(composition);");
                    }

                    code.AppendLine("}");

                    code.AppendLine();
                    code.AppendLine($"object {objectTypeName}.{Names.ResolveByTagMethodName}({composition.Source.Source.Name.ClassName} composition, object tag)");
                    code.AppendLine("{");
                    using (code.Indent())
                    {
                        code.AppendLine($"return {Names.ResolveByTagMethodName}(composition, tag);");
                    }

                    code.AppendLine("}");
                }
            }

            code.AppendLine("}");
            membersCount++;
        }

        code.AppendLine("#endregion");
        return composition with { MembersCount = membersCount };
    }

    private static void ImplementInterface(CompositionCode composition, ResolverInfo resolver, LinesBuilder code)
    {
        var defaultRoot = resolver.Roots.SingleOrDefault(i => i.Injection.Tag is null);
        code.AppendLine($"public override {resolver.Type} {Names.ResolveMethodName}({composition.Source.Source.Name.ClassName} composition)");
        code.AppendLine("{");
        using (code.Indent())
        {
            if (defaultRoot is not null)
            {
                var isStatic = (defaultRoot.Kind & RootKinds.Static) == RootKinds.Static;
                var isMethod = !defaultRoot.Args.IsEmpty || (defaultRoot.Kind & RootKinds.Method) == RootKinds.Method;
                code.AppendLine($"return {(isStatic ? composition.Source.Source.Name.ClassName : "composition")}.{defaultRoot.DisplayName}{(isMethod ? "()" : "")};");
            }
            else
            {
                code.AppendLine($"return base.{Names.ResolveMethodName}(composition);");
            }
        }

        code.AppendLine("}");

        code.AppendLine();

        code.AppendLine($"public override {resolver.Type} {Names.ResolveByTagMethodName}({composition.Source.Source.Name.ClassName} composition, object tag)");
        code.AppendLine("{");
        using (code.Indent())
        {
            var taggedRoots = resolver.Roots;
            foreach (var taggedRoot in taggedRoots.Where(i => !CanBeUsedInSwitch(i)))
            {
                code.AppendLine($"if (Equals(tag, {taggedRoot.Injection.Tag.ValueToString()})) return {GetRoot(composition, taggedRoot)};");
                code.AppendLine();
            }

            code.AppendLine("switch (tag)");
            code.AppendLine("{");
            using (code.Indent())
            {
                foreach (var taggedRoot in taggedRoots.Where(CanBeUsedInSwitch).OrderBy(i => i.Injection.Tag is null))
                {
                    code.AppendLine($"case {taggedRoot.Injection.Tag.ValueToString()}:");
                    using (code.Indent())
                    {
                        code.AppendLine($"return {GetRoot(composition, taggedRoot)};");
                    }

                    code.AppendLine();
                }

                code.AppendLine("default:");
                using (code.Indent())
                {
                    code.AppendLine($"return base.{Names.ResolveByTagMethodName}(composition, tag);");
                }
            }

            code.AppendLine("}");
        }

        code.AppendLine("}");
    }

    [SuppressMessage("ReSharper", "ConvertIfStatementToReturnStatement")]
    private static bool CanBeUsedInSwitch(Root root)
    {
        if (root.Injection.Tag?.GetType() is not { } tagType)
        {
            return true;
        }

        if (tagType.IsPrimitive)
        {
            return true;
        }

        if (tagType.IsEnum)
        {
            return true;
        }

        if (tagType == typeof(string))
        {
            return true;
        }

        return false;
    }

    private static string GetRoot(CompositionCode composition, Root root)
    {
        var target = (root.Kind & RootKinds.Static) == RootKinds.Static ? composition.Source.Source.Name.ClassName : "composition";
        var isMethod = !root.Args.IsEmpty || (root.Kind & RootKinds.Method) == RootKinds.Method;
        return $"{target}.{root.DisplayName}{(isMethod ? "()" : "")}";
    }
}