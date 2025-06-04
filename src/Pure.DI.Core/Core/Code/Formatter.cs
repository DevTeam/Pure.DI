// ReSharper disable ClassNeverInstantiated.Global

#pragma warning disable CS9113 // Parameter is unread.
namespace Pure.DI.Core.Code;

sealed class Formatter(
    IComments comments,
    ITypeResolver typeResolver)
    : IFormatter
{
#if ROSLYN4_8_OR_GREATER
    private static readonly SymbolDisplayFormat RefFormat = new(
        genericsOptions:
        SymbolDisplayGenericsOptions.IncludeTypeParameters,
        typeQualificationStyle:
        SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        memberOptions:
        SymbolDisplayMemberOptions.IncludeType |
        SymbolDisplayMemberOptions.IncludeContainingType,
        miscellaneousOptions:
        SymbolDisplayMiscellaneousOptions.ExpandValueTuple |
        SymbolDisplayMiscellaneousOptions.UseSpecialTypes |
        SymbolDisplayMiscellaneousOptions.ExpandNullable
    );
#endif

    public string Format(Root root)
    {
        var sb = new StringBuilder();
        sb.Append(root.DisplayName);
        if (!root.IsMethod)
        {
            return sb.ToString();
        }

        var typeArgs = root.TypeDescription.TypeArgs;
        if (typeArgs.Count > 0)
        {
            sb.Append("&lt;");
            sb.Append(string.Join(", ", typeArgs.Select(i => i.Name)));
            sb.Append("&gt;");
        }

        sb.Append('(');
        sb.Append(string.Join(", ", root.RootArgs.Select(i => i.Name)));
        sb.Append(')');
        return sb.ToString();
    }

    public string FormatRef(MdSetup setup, Root root)
    {
        var sb = new StringBuilder();
        sb.Append(root.DisplayName);
        if (!root.IsMethod)
        {
            return FormatRef(sb.ToString());
        }

        var typeArgs = root.TypeDescription.TypeArgs;
        if (typeArgs.Count > 0)
        {
            sb.Append('{');
            sb.Append(string.Join(", ", typeArgs.Select(i => FormatTypeName(i.Name))));
            sb.Append('}');
        }

        sb.Append('(');
        sb.Append(string.Join(", ", root.RootArgs.Select(i => FormatTypeName(typeResolver.Resolve(setup, i.InstanceType).Name))));
        sb.Append(')');
        return FormatRef(sb.ToString());
    }

    public string FormatRef(string text) =>
        $"<see cref=\"{text}\"/>";

    public string FormatRef(Lifetime lifetime) =>
        $"<see cref=\"{Names.GeneratorName}.{nameof(Lifetime)}.{lifetime}\"/>";

    public string FormatRef(ITypeSymbol type)
    {
#if ROSLYN4_8_OR_GREATER
        var originalDefinitionTypeName = type.OriginalDefinition.ToDisplayString(NullableFlowState.None, RefFormat);
        return FormatRef(FormatTypeName(originalDefinitionTypeName));
#else
        return comments.Escape(type.OriginalDefinition.ToDisplayString(NullableFlowState.None, SymbolDisplayFormat.MinimallyQualifiedFormat));
#endif
    }

    private string FormatTypeName(string typeName) =>
        comments.Escape(typeName.Replace('<', '{').Replace('>', '}'));
}