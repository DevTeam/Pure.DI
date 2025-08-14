namespace Pure.DI.Core.Code.Parts;

class RootSignatureProvider(
    IRootAccessModifierResolver rootAccessModifierResolver,
    ITypeResolver typeResolver)
    : IRootSignatureProvider
{
    private const string ClassConstraint = "class";
    private const string UnmanagedConstraint = "unmanaged";
    private const string NotnullConstraint = "notnull";
    private const string StructConstraint = "struct";
    private const string NewConstraint = "new()";

    public string GetRootSignature(CompositionCode composition, Root root)
    {
        var rootSignature = new StringBuilder();
        rootSignature.Append(GetAccessModifier(root));
        if ((root.Kind & RootKinds.Static) == RootKinds.Static)
        {
            rootSignature.Append(" static");
        }

        if ((root.Kind & RootKinds.Partial) == RootKinds.Partial)
        {
            rootSignature.Append(" partial");
        }

        if ((root.Kind & RootKinds.Virtual) == RootKinds.Virtual)
        {
            rootSignature.Append(" virtual");
        }

        if ((root.Kind & RootKinds.Override) == RootKinds.Override)
        {
            rootSignature.Append(" override");
        }

        rootSignature.Append(' ');
        rootSignature.Append(root.TypeDescription.Name);

        rootSignature.Append(' ');
        rootSignature.Append(root.DisplayName);

        var typeArgs = root.TypeDescription.TypeArgs;
        if (typeArgs.Count > 0)
        {
            rootSignature.Append('<');
            rootSignature.Append(string.Join(", ", typeArgs));
            rootSignature.Append('>');
        }

        if (root.IsMethod)
        {
            rootSignature.Append($"({string.Join(", ", root.RootArgs.Select(arg => $"{typeResolver.Resolve(composition.Source.Source, arg.InstanceType)} {arg.Name}"))})");
        }

        return rootSignature.ToString();
    }

    public ImmutableDictionary<TypeDescription, ImmutableArray<string>>? TryGetConstraints(CompositionCode composition, Root root)
    {
        var typeArgs = root.TypeDescription.TypeArgs;
        var result = ImmutableDictionary<TypeDescription, ImmutableArray<string>>.Empty;
        if (typeArgs.Count == 0)
        {
            return result;
        }

        foreach (var typeArg in typeArgs.GroupBy(typeArg => typeArg.Name).Select(i => i.First()))
        {
            if (typeArg.TypeParam is not {} curTypeParam)
            {
                continue;
            }

            var typeParameters = ImmutableArray<ITypeParameterSymbol>.Empty;
            if (!root.Source.BuilderRoots.IsDefaultOrEmpty)
            {
                var relatedRoots =
                    from publicRoot in composition.PublicRoots
                    join relatedRoot in root.Source.BuilderRoots on publicRoot.Source equals relatedRoot
                    select publicRoot;

                typeParameters = relatedRoots.SelectMany(i => i.TypeDescription.TypeArgs).Where(i => i.Name == typeArg.Name && i.TypeParam != null).Select(i => i.TypeParam!).ToImmutableArray();
            }

            var constraints = new List<string>();
            constraints.AddRange(curTypeParam.ConstraintTypes.Select(i => typeResolver.Resolve(composition.Source.Source, i).Name).OrderBy(i => i));
            foreach (var typeParameter in typeParameters)
            {
                constraints.AddRange(typeParameter.ConstraintTypes.Select(i => typeResolver.Resolve(composition.Source.Source, i).Name).OrderBy(i => i));
            }

            FillConstraints(curTypeParam, constraints);
            foreach (var typeParameter in typeParameters)
            {
                FillConstraints(typeParameter, constraints);
            }

            if (constraints.Count == 0)
            {
                continue;
            }

            constraints = constraints.Distinct().ToList();
            if (constraints.Contains(ClassConstraint) && constraints.Contains(StructConstraint))
            {
                return null;
            }

            result = result.Add(typeArg, constraints.ToImmutableArray());
        }

        return result;
    }

    private string GetAccessModifier(Root root) =>
        rootAccessModifierResolver.Resolve(root) switch
        {
            Accessibility.Private => "private",
            Accessibility.ProtectedAndInternal => "protected",
            Accessibility.Protected => "protected",
            Accessibility.Internal => "internal",
            Accessibility.ProtectedOrInternal => "internal",
            Accessibility.Public => "public",
            _ => ""
        };


    private static void FillConstraints(ITypeParameterSymbol typeParam, List<string> constrains)
    {
        if (typeParam.HasReferenceTypeConstraint)
        {
            constrains.Add(ClassConstraint);
        }

        if (typeParam.HasUnmanagedTypeConstraint)
        {
            constrains.Add(UnmanagedConstraint);
        }

        if (typeParam.HasNotNullConstraint)
        {
            constrains.Add(NotnullConstraint);
        }

        if (typeParam.HasValueTypeConstraint)
        {
            constrains.Add(StructConstraint);
        }

        if (typeParam.HasConstructorConstraint)
        {
            constrains.Add(NewConstraint);
        }
    }
}