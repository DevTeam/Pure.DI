// ReSharper disable MemberCanBeMadeStatic.Global
namespace Build.Core.Doc;

using System.Text;

class MarkdownParts
{
    [SuppressMessage("Performance", "CA1822:Mark members as static")]
    public Memory<string> GetParts(string? text)
    {
        var result = new List<string>();
        if (text is null)
        {
            return Memory<string>.Empty;
        }

        var sb = new StringBuilder();
        var isArgs = false;
        foreach (var symbol in text)
        {
            switch (symbol)
            {
                case '.' when !isArgs:
                    result.Add(sb.ToString());
                    sb.Clear();
                    continue;

                case '(':
                    isArgs = true;
                    break;
            }

            sb.Append(symbol);
        }

        if (sb.Length > 0)
        {
            result.Add(sb.ToString());
        }

        return result.ToArray();
    }

    [SuppressMessage("Performance", "CA1822:Mark members as static")] public string Join(ReadOnlyMemory<string> parts) => string.Join('.', parts.ToArray());
}