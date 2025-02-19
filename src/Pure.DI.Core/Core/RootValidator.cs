﻿// ReSharper disable ClassNeverInstantiated.Global

namespace Pure.DI.Core;

sealed class RootValidator(
    ILogger logger)
    : IValidator<CompositionCode>
{
    public bool Validate(CompositionCode composition)
    {
        var hints = composition.Source.Source.Hints;
        if (!hints.IsResolveEnabled)
        {
            return true;
        }

        var rootArgs = composition.Roots
            .Where(root => !root.Source.IsBuilder)
            .Select(root => (root, args: root.Args.GetArgsOfKind(ArgKind.Root).ToList()))
            .Where(i => i.args.Count > 0)
            .GroupBy(i => i.root.Node.Binding.Id)
            .Select(i => i.First());

        foreach (var root in rootArgs)
        {
            logger.CompileWarning(
                string.Format(Strings.Warning_Template_RootCannotBeResolvedByResolveMethods, Format(root.root), string.Join(", ", root.args.Select(i => i.VariableName))),
                root.root.Node.Arg?.Source.Source.GetLocation() ?? composition.Source.Source.Source.GetLocation(),
                LogId.WarningRootArgInResolveMethod);
        }

        var genericRoots = composition.Roots
            .Where(root => !root.Source.IsBuilder)
            .Where(i => i.TypeDescription.TypeArgs.Count > 0)
            .GroupBy(i => i.Node.Binding.Id)
            .Select(i => i.First());

        foreach (var root in genericRoots)
        {
            logger.CompileWarning(
                string.Format(Strings.Warning_Template_RootCannotBeResolvedByResolveMethods, Format(root), string.Join(", ", root.TypeDescription.TypeArgs)),
                root.Node.Arg?.Source.Source.GetLocation() ?? composition.Source.Source.Source.GetLocation(),
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
        sb.Append(string.Join(", ", root.Args.Select(i => i.VariableDeclarationName)));
        sb.Append(')');
        return sb.ToString();
    }
}