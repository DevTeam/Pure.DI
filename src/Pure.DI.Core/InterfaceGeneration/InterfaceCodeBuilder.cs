// ReSharper disable InvertIf
namespace Pure.DI.InterfaceGeneration;

using System;
using System.Linq;
using System.Text;
using Core;
using Core.Code.Parts;
using Microsoft.CodeAnalysis;

sealed class InterfaceCodeBuilder(
    IFileHeader fileHeader,
    IInformation information)
    : IBuilder<GeneratedInterfaceDetails, Lines>
{
    public Lines Build(GeneratedInterfaceDetails ctx)
    {
        var lines = new Lines();
        fileHeader.Add(ctx.SemanticModel.Compilation, lines);

        lines.AppendLine("using System;");
        lines.AppendLine();

        var namespaceName = ctx.NamespaceName;
        if (!string.IsNullOrWhiteSpace(namespaceName))
        {
            lines.AppendLine($"namespace {namespaceName}");
            lines.AppendLine("{");
            lines.IncIndent();
        }

        AppendAndNormalizeMultipleLines(lines, ctx.ClassDocumentation);
        var accessLevel = ctx.AccessLevel;
        var interfaceName = ctx.InterfaceName;
        lines.AppendLine("#if !NET20 && !NET35 && !NETSTANDARD1_0 && !NETSTANDARD1_1 && !NETSTANDARD1_2 && !NETSTANDARD1_3 && !NETSTANDARD1_4 && !NETSTANDARD1_5 && !NETSTANDARD1_6 && !NETCOREAPP1_0 && !NETCOREAPP1_1");
        lines.AppendLine($"[global::System.CodeDom.Compiler.GeneratedCode({SymbolDisplay.FormatLiteral(information.Name, true)}, {SymbolDisplay.FormatLiteral(information.Version, true)})]");
        lines.AppendLine("#endif");
        lines.AppendLine($"{accessLevel} partial interface {interfaceName}{ctx.GenericType}");
        lines.AppendLine("{");
        lines.IncIndent();

        foreach (var prop in ctx.PropertyInfos)
        {
            AppendAndNormalizeMultipleLines(lines, prop.Documentation);
            var @ref = prop.IsRef ? "ref " : string.Empty;
            var get = prop.HasGet ? "get; " : string.Empty;
            var set = GetSet(prop.SetKind);
            lines.AppendLine($"{@ref}{prop.Type} {prop.Name} {{ {get}{set}}}");
            lines.AppendLine();
        }

        foreach (var method in ctx.MethodInfos)
        {
            BuildMethod(lines, method);
        }

        foreach (var evt in ctx.Events)
        {
            AppendAndNormalizeMultipleLines(lines, evt.Documentation);
            lines.AppendLine($"event {evt.Type} {evt.Name};");
            lines.AppendLine();
        }

        lines.DecIndent();
        lines.AppendLine("}");

        if (!string.IsNullOrWhiteSpace(namespaceName))
        {
            lines.DecIndent();
            lines.AppendLine("}");
        }

        return lines;
    }

    private static string GetSet(PropertySetKind propSetKind) =>
        propSetKind switch
        {
            PropertySetKind.NoSet => string.Empty,
            PropertySetKind.Always => "set; ",
            PropertySetKind.Init => "init; ",
            _ => throw new ArgumentOutOfRangeException(nameof(propSetKind), propSetKind, null)
        };

    private static void BuildMethod(Lines lines, MethodInfo method)
    {
        AppendAndNormalizeMultipleLines(lines, method.Documentation);
        var sb = new StringBuilder($"{method.ReturnType} {method.Name}");

        if (method.GenericArgs.Length > 0)
        {
            sb.Append($"<{string.Join(", ", method.GenericArgs.Select(a => a.Arg))}>");
        }

        sb.Append($"({string.Join(", ", method.Parameters)})");

        if (method.GenericArgs.Length > 0)
        {
            var constraints = method.GenericArgs
                .Where(a => !string.IsNullOrWhiteSpace(a.WhereConstraint))
                .Select(a => a.WhereConstraint);
            sb.Append($" {string.Join(" ", constraints)}");
        }

        sb.Append(';');
        lines.AppendLine(sb.ToString());
        lines.AppendLine();
    }

    private static void AppendAndNormalizeMultipleLines(Lines lines, string doc)
    {
        if (string.IsNullOrWhiteSpace(doc))
        {
            return;
        }

        foreach (var line in doc.Split([Environment.NewLine], StringSplitOptions.None))
        {
            lines.AppendLine(line.TrimStart());
        }
    }
}
