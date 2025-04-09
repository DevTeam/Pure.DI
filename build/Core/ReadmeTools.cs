// ReSharper disable MemberCanBeMadeStatic.Global
namespace Build.Core;

class ReadmeTools
{
    public IEnumerable<char> FormatTitle(string title)
    {
        var isFirst = true;
        foreach (var ch in title)
        {
            if (!isFirst && char.IsUpper(ch))
            {
                yield return ' ';
            }

            yield return ch;
            isFirst = false;
        }
    }
}