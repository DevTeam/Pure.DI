namespace Pure.DI.Core
{
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.Text;

    internal readonly struct Source
    {
        public readonly string HintName;
        public readonly SourceText Code;
        public readonly SyntaxTree SyntaxTree;

        public Source(string hintName, SourceText code, SyntaxTree syntaxTree)
        {
            HintName = hintName;
            Code = code;
            SyntaxTree = syntaxTree;
        }
    }
}
