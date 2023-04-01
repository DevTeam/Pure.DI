namespace Pure.DI.Core.CSharp;

internal class ApiMembersBuilder: IBuilder<CompositionCode, CompositionCode>
{
    internal static readonly string ResolverMethodName = "Resolve";
    internal static readonly string ResolveMethodName = nameof(IResolver<object>.ObjectResolve);
    internal static readonly string ResolveByTagMethodName = nameof(IResolver<object>.ObjectResolveByTag);

    public CompositionCode Build(CompositionCode composition, CancellationToken cancellationToken)
    {
        var code = composition.Code;
        var membersCounter = composition.MembersCount;
        if (composition.MembersCount > 0)
        {
            code.AppendLine();
        }
        
        code.AppendLine("#region API");
        AddMethodHeader(code);
        code.AppendLine($"public T {ResolverMethodName}<T>()");
        code.AppendLine("{");
        using (code.Indent())
        {
            code.AppendLine($"return {ResolverClassesBuilder.ResolverClassName}<T>.{ResolverClassesBuilder.ResolverPropertyName}!.{ResolverClassesBuilder.ResolveMethodName}(this);");
        }
        code.AppendLine("}");
        
        code.AppendLine();
        membersCounter++;
        
        AddMethodHeader(code);
        code.AppendLine($"public T {ResolverMethodName}<T>(object? tag)");
        code.AppendLine("{");
        using (code.Indent())
        {
            code.AppendLine($"return {ResolverClassesBuilder.ResolverClassName}<T>.{ResolverClassesBuilder.ResolverPropertyName}!.{ResolverClassesBuilder.ResolveByTagMethodName}(this, tag);");
        }
        code.AppendLine("}");
        
        code.AppendLine();
        membersCounter++;
        
        var roots = composition.Roots.GetActualRoots().ToArray();
        
        CreateObjectResolverMethod(composition, roots, "System.Type type", ResolveMethodName, "this", code);
        membersCounter++;
        code.AppendLine();
        
        CreateObjectResolverMethod(composition, roots, "System.Type type, object? tag", ResolveByTagMethodName, "this, tag", code);
        membersCounter++;
        code.AppendLine();
        
        code.AppendLine(CodeConstants.MethodImplOptions);
        code.AppendLine($"partial void {CodeConstants.OnInstanceCreationMethodName}<T>(ref T value, object? tag, object? lifetime);");
        membersCounter++;

        if (composition.Source.Source.Settings.GetBool(Setting.TrackInjections))
        {
            code.AppendLine(CodeConstants.MethodImplOptions);
            code.AppendLine($"private partial T {CodeConstants.OnDependencyInjectionMethodName}<T>(in T value, object? tag, object? lifetime);");
            membersCounter++;
        }
        
        code.AppendLine("#endregion");
        return composition with { MembersCount = membersCounter };
    }

    private static void CreateObjectResolverMethod(
        CompositionCode composition,
        IEnumerable<Root> roots,
        string methodArgs,
        string resolveMethodName,
        string resolveMethodArgs,
        LinesBuilder code)
    {
        AddMethodHeader(code);
        code.AppendLine($"public object {ResolverMethodName}({methodArgs})");
        code.AppendLine("{");
        using (code.Indent())
        {
            var actualRoots = roots.Where(i => i.Injection.Tag is not { }).ToArray();
            var divisor = Buckets<object, object>.GetDivisor((uint)actualRoots.Length);
            if (actualRoots.Any())
            {
                var pairs = $"System.Type, {ResolverClassesBuilder.ResolverInterfaceName}<{composition.Name.ClassName}>";
                var pairTypeName = $"{CodeConstants.ApiNamespace}Pair<{pairs}>";
                if (divisor <= 1)
                {
                    code.AppendLine($"{pairTypeName} pair = {ResolversFieldsBuilder.BucketsFieldName}[0U];");
                }
                else
                {
                    code.AppendLine("#if NETSTANDARD || NETCOREAPP");
                    code.AppendLine($"{pairTypeName} pair = {ResolversFieldsBuilder.BucketsFieldName}[(uint)System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(type) % {divisor}];");
                    code.AppendLine("#else");
                    code.AppendLine($"{pairTypeName} pair = {ResolversFieldsBuilder.BucketsFieldName}[(uint)type.GetHashCode() % {divisor}];");
                    code.AppendLine("#endif");
                }

                code.AppendLine("do");
                code.AppendLine("{");
                using (code.Indent())
                {
                    code.AppendLine("if (ReferenceEquals(type, pair.Key))");
                    code.AppendLine("{");
                    using (code.Indent())
                    {
                        code.AppendLine($"return pair.Value.{resolveMethodName}({resolveMethodArgs});");
                    }

                    code.AppendLine("}");
                    code.AppendLine();
                    code.AppendLine("pair = pair.Next;");
                }

                code.AppendLine("} while (pair != null);");
                code.AppendLine();
            }

            code.AppendLine($"throw new System.InvalidOperationException($\"{CodeConstants.CannotResolve} of type {{type}}.\");");
        }
        
        code.AppendLine("}");
    }

    private static void AddMethodHeader(LinesBuilder code)
    {
        code.AppendLine("#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP || NET40_OR_GREATER");
        code.AppendLine("[System.Diagnostics.Contracts.Pure]");
        code.AppendLine("#endif");
        code.AppendLine(CodeConstants.MethodImplOptions);
    }
}