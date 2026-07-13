// ReSharper disable ClassNeverInstantiated.Global

#pragma warning disable RS1024 // Pure.DI intentionally uses ITypeSymbolComparer to control nullable-reference contract equality.

namespace Pure.DI.Core;

sealed class RootValidator(
    ILogger logger,
    ILocationProvider locationProvider,
    ITypeSymbolComparer typeSymbolComparer)
    : IValidator<CompositionCode>
{
    public bool Validate(CompositionCode composition)
    {
        var hints = composition.Hints;
        if (!hints.IsResolveEnabled)
        {
            return true;
        }

        var invalidRoots = composition.PublicRoots
            .Where(root => root.Source is { IsBuilder: false, LightweightKind: not LightweightKind.RootsProvider })
            .Where(root => !root.RootArgs.IsDefaultOrEmpty && root.TypeDescription.TypeArgs.Count == 0)
            .GroupBy(root => root.Node.Binding.Id)
            .Select(root => root.First());

        foreach (var invalidRoot in invalidRoots)
        {
            logger.CompileWarning(
                LogMessage.Format(
                    nameof(Strings.Warning_Template_RootCannotBeResolvedByResolveMethods),
                    Strings.Warning_Template_RootCannotBeResolvedByResolveMethods,
                    Format(invalidRoot),
                    string.Join(", ", invalidRoot.RootArgs.Select(i => i.Name))),
                ImmutableArray.Create(locationProvider.GetLocation(invalidRoot.Source.Source)),
                LogId.WarningRootArgInResolveMethod);
        }

        var genericRoots = composition.PublicRoots
            .Where(root => !root.Source.IsBuilder)
            .Where(i => i.TypeDescription.TypeArgs.Count > 0)
            .GroupBy(i => i.Node.Binding.Id)
            .Select(i => i.First());

        foreach (var root in genericRoots)
        {
            logger.CompileWarning(
                LogMessage.Format(
                    nameof(Strings.Warning_Template_RootCannotBeResolvedByResolveMethods),
                    Strings.Warning_Template_RootCannotBeResolvedByResolveMethods,
                    Format(root),
                    string.Join(", ", root.TypeDescription.TypeArgs)),
                ImmutableArray.Create(locationProvider.GetLocation(root.Source.Source)),
                LogId.WarningTypeArgInResolveMethod);
        }

        var nullableRuntimeRootGroups = composition.PublicRoots
            .Where(IsRuntimeResolvableRoot)
            .GroupBy(root => root.Injection.Type, typeSymbolComparer.Runtime)
            .Where(group => group.Any(root => HasNullableReferenceAnnotation(root.Injection.Type)))
            .Where(group => group.Select(root => root.Injection.Type).Distinct(typeSymbolComparer.Dependency).Skip(1).Any());

        foreach (var root in nullableRuntimeRootGroups.SelectMany(group => group))
        {
            logger.CompileWarning(
                LogMessage.Format(
                    nameof(Strings.Warning_Template_NullableRootCannotBeDistinguishedByResolveTypeMethods),
                    Strings.Warning_Template_NullableRootCannotBeDistinguishedByResolveTypeMethods,
                    Format(root)),
                ImmutableArray.Create(locationProvider.GetLocation(root.Source.Source)),
                LogId.WarningNullableRootInResolveMethod);
        }

        return true;
    }

    private static bool IsRuntimeResolvableRoot(Root root) =>
        root is { Source: { IsBuilder: false, LightweightKind: not LightweightKind.RootsProvider }, RootArgs.IsDefaultOrEmpty: true, Injection.Type.IsRefLikeType: false }
        && !ReferenceEquals(root.Injection.Tag, MdTag.ContextTag)
        && root.TypeDescription.TypeArgs.Count == 0;

    private static bool HasNullableReferenceAnnotation(ITypeSymbol type) =>
        type switch
        {
            { IsReferenceType: true, NullableAnnotation: NullableAnnotation.Annotated } => true,
            INamedTypeSymbol namedType => namedType.TypeArguments.Any(HasNullableReferenceAnnotation),
            IArrayTypeSymbol arrayType => HasNullableReferenceAnnotation(arrayType.ElementType),
            _ => false
        };

    private static string Format(Root root)
    {
        var sb = new StringBuilder();
        sb.Append(root.TypeDescription);
        if (root.IsPublic)
        {
            sb.Append(' ');
            sb.Append(root.DisplayName);
        }

        if (!root.IsMethod)
        {
            return sb.ToString();
        }

        var typeArgs = root.TypeDescription.TypeArgs;
        if (typeArgs.Count > 0)
        {
            sb.Append("<");
            sb.Append(string.Join(", ", typeArgs.Select(i => i.Name)));
            sb.Append(">");
        }

        sb.Append('(');
        sb.Append(string.Join(", ", root.RootArgs.Select(i => i.Name)));
        sb.Append(')');
        return sb.ToString();
    }
}
