// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core.Code;

internal class Formatter(
    ITypeResolver typeResolver,
    IComments comments)
    : IFormatter
{
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
        sb.Append(string.Join(", ", root.Args.Select(i => i.VariableName)));
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
    
    public string FormatRef(string text) =>
        $"<see cref=\"{text}\"/>";

    public string FormatRef(ITypeSymbol type) =>
        FormatRef(
            comments.Escape(
                typeResolver.Resolve(type).Name
                    .Replace('<', '{')
                    .Replace('>', '}')));
}