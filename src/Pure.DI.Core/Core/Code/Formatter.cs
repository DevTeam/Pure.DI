// ReSharper disable ClassNeverInstantiated.Global
#pragma warning disable CS9113 // Parameter is unread.
namespace Pure.DI.Core.Code;

internal class Formatter(IComments comments)
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
        
        var typeArgs= root.TypeDescription.TypeArgs;
        if (typeArgs.Count > 0)
        {
            sb.Append("&lt;");
            sb.Append(string.Join(", ", typeArgs.Select(i => i.Name)));
            sb.Append("&gt;");    
        }
            
        sb.Append('(');
        sb.Append(string.Join(", ", root.Args.Select(i => i.VariableDeclarationName)));
        sb.Append(')');
        return sb.ToString();
    }
    
    public string FormatRef(Root root)
    {
        var sb = new StringBuilder();
        sb.Append(root.DisplayName);
        if (!root.IsMethod)
        {
            return FormatRef(sb.ToString());
        }
        
        var typeArgs= root.TypeDescription.TypeArgs;
        if (typeArgs.Count > 0)
        {
            sb.Append('{');
            sb.Append(string.Join(", ", typeArgs.Select(i => i.Name)));
            sb.Append('}');
        }
            
        sb.Append('(');
        sb.Append(string.Join(", ", root.Args.Select(i => i.ContractType)));
        sb.Append(')');
        return FormatRef(sb.ToString());
    }
    
    public string FormatRef(string text)
    {
        return $"<see cref=\"{text}\"/>";
    }

    public string FormatRef(ITypeSymbol type)
    {
#if ROSLYN4_8_OR_GREATER
        var originalDefinitionTypeName = type.OriginalDefinition.ToDisplayString(NullableFlowState.None, RefFormat);
        return FormatRef(comments.Escape(originalDefinitionTypeName.Replace('<', '{').Replace('>', '}')));
#else
        return comments.Escape(type.OriginalDefinition.ToDisplayString(NullableFlowState.None, SymbolDisplayFormat.MinimallyQualifiedFormat));
#endif
    }
}