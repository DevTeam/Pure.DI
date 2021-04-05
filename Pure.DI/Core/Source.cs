namespace Pure.DI.Core
{
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.Text;

    internal readonly struct Source
    {
        public readonly string HintName;
        public readonly SourceText Code;
        public readonly SyntaxTree Tree;

        public Source(string hintName, SourceText code, SyntaxTree tree)
        {
            HintName = hintName;
            Code = code;
            Tree = tree;
        }
    }
}
