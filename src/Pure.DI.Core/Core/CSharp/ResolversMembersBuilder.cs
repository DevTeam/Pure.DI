namespace Pure.DI.Core.CSharp;

internal class ResolversMembersBuilder: IBuilder<CompositionCode, CompositionCode>
{
    internal static readonly string ResolverMethodName = "Resolve";
    
    public CompositionCode Build(CompositionCode composition, CancellationToken cancellationToken)
    {
        var code = composition.Code;
        var membersCounter = composition.MembersCount;
        if (composition.MembersCount > 0)
        {
            code.AppendLine();
        }
        
        code.AppendLine("#region Resolvers");
        AddMethodHeader(code);
        code.AppendLine($"public T {ResolverMethodName}<T>()");
        code.AppendLine("{");
        using (code.Indent())
        {
            code.AppendLine($"return {ResolverClassBuilder.ResolverClassName}<T>.{ResolverClassBuilder.ResolveMethodName}(this);");
        }
        code.AppendLine("}");
        
        code.AppendLine();
        membersCounter++;
        
        AddMethodHeader(code);
        code.AppendLine($"public T {ResolverMethodName}<T>(object tag)");
        code.AppendLine("{");
        using (code.Indent())
        {
            code.AppendLine($"return {ResolverClassBuilder.ResolverClassName}<T>.{ResolverClassBuilder.ResolveByTagMethodName}(this, tag);");
        }
        code.AppendLine("}");
        
        code.AppendLine();
        membersCounter++;
        
        var roots = composition.Roots.GetActualRoots().ToArray();
        AddMethodHeader(code);
        code.AppendLine($"public object {ResolverMethodName}(Type type, object tag)");
        code.AppendLine("{");
        using (code.Indent())
        {
            var actualRoots = roots.Where(i => i.Injection.Tag is {}).ToArray();
            if (actualRoots.Any())
            {
                var divisor = Buckets<object, object>.GetDivisor((uint)actualRoots.Length);
                var pairs = $"Pure.DI.RootKey, System.Func<{composition.ClassName}, object>";
                var pairTypeName = $"Pure.DI.Pair<{pairs}>";
                if (divisor <= 1)
                {
                    code.AppendLine("const uint bucket = 0U;");    
                }
                else
                {
                    code.AppendLine("uint bucket;");
                    code.AppendLine("unchecked");
                    code.AppendLine("{");
                    using (code.Indent())
                    {
                        code.AppendLine("#if NETSTANDARD || NETCOREAPP");
                        code.AppendLine($"bucket = ((uint)((tag.GetHashCode() * 397) ^ System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(type))) % {divisor};");
                        code.AppendLine("#else");
                        code.AppendLine($"bucket = ((uint)((tag.GetHashCode() * 397) ^ type.GetHashCode())) % {divisor};");
                        code.AppendLine("#endif");
                    }

                    code.AppendLine("}");
                }
                
                code.AppendLine();
                code.AppendLine($"{pairTypeName} pair = {ResolversFieldsBuilder.BucketsByTagFieldName}[bucket];");
                code.AppendLine("do");
                code.AppendLine("{");
                using (code.Indent())
                {
                    code.AppendLine("if (ReferenceEquals(type, pair.Key.Type) && Equals(tag, pair.Key.Tag))");
                    code.AppendLine("{");
                    using (code.Indent())
                    {
                        code.AppendLine("return pair.Value(this);");
                    }

                    code.AppendLine("}");
                    code.AppendLine();
                    code.AppendLine("pair = pair.Next;");
                }

                code.AppendLine("} while (pair != null);");
                code.AppendLine();
            }

            code.AppendLine($"throw new System.InvalidOperationException($\"{CodeExtensions.CannotResolve} \\\"{{tag}}\\\" of type {{type}}.\");");
        }
        code.AppendLine("}");
        
        code.AppendLine();
        membersCounter++;
        
        AddMethodHeader(code);
        code.AppendLine($"public object {ResolverMethodName}(Type type)");
        code.AppendLine("{");
        using (code.Indent())
        {
            var actualRoots = roots.Where(i => i.Injection.Tag is not {}).ToArray();
            var divisor = Buckets<object, object>.GetDivisor((uint)actualRoots.Length);
            if (actualRoots.Any())
            {
                var pairs = $"Pure.DI.RootKey, System.Func<{composition.ClassName}, object>";
                var pairTypeName = $"Pure.DI.Pair<{pairs}>";
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
                    code.AppendLine("if (ReferenceEquals(type, pair.Key.Type))");
                    code.AppendLine("{");
                    using (code.Indent())
                    {
                        code.AppendLine("return pair.Value(this);");
                    }

                    code.AppendLine("}");
                    code.AppendLine();
                    code.AppendLine("pair = pair.Next;");
                }

                code.AppendLine("} while (pair != null);");
                code.AppendLine();
            }

            code.AppendLine($"throw new System.InvalidOperationException($\"{CodeExtensions.CannotResolve} of type {{type}}.\");");
        }
        code.AppendLine("}");
        code.AppendLine("#endregion");
        return composition with { MembersCount = membersCounter };
    }

    private static void AddMethodHeader(LinesBuilder code)
    {
        code.AppendLine("#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP || NET40_OR_GREATER");
        code.AppendLine("[System.Diagnostics.Contracts.Pure]");
        code.AppendLine("#endif");
        code.AppendLine(CodeExtensions.MethodImplOptions);
    }
}