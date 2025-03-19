namespace Pure.DI.Core.Code;

interface ILocalVariableRenamingRewriter
{
    SyntaxNode Rewrite(SemanticModel semanticModel, bool formatCode, bool isOverride, SyntaxNode lambda);

    ILocalVariableRenamingRewriter Clone();
}