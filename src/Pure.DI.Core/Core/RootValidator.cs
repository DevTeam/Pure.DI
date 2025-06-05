// ReSharper disable ClassNeverInstantiated.Global

namespace Pure.DI.Core;

sealed class RootValidator(
    ILogger logger,
    ILocationProvider locationProvider)
    : IValidator<CompositionCode>
{
    public bool Validate(CompositionCode composition)
    {
        var hints = composition.Source.Source.Hints;
        if (!hints.IsResolveEnabled)
        {
            return true;
        }

        var invalidRoots = composition.PublicRoots
            .Where(root => !root.Source.IsBuilder)
            .Where(root => !root.RootArgs.IsDefaultOrEmpty && root.TypeDescription.TypeArgs.Count == 0)
            .GroupBy(root => root.Node.Binding.Id)
            .Select(root => root.First());

        foreach (var invalidRoot in invalidRoots)
        {
            logger.CompileWarning(
                string.Format(Strings.Warning_Template_RootCannotBeResolvedByResolveMethods, Format(invalidRoot), string.Join(", ", invalidRoot.RootArgs.Select(i => i.Name))),
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
                string.Format(Strings.Warning_Template_RootCannotBeResolvedByResolveMethods, Format(root), string.Join(", ", root.TypeDescription.TypeArgs)),
                ImmutableArray.Create(locationProvider.GetLocation(root.Source.Source)),
                LogId.WarningTypeArgInResolveMethod);
        }

        return true;
    }

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