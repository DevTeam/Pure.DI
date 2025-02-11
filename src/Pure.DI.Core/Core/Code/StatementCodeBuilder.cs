// ReSharper disable RedundantJumpStatement
// ReSharper disable InvertIf
// ReSharper disable ClassNeverInstantiated.Global

namespace Pure.DI.Core.Code;

internal sealed class StatementCodeBuilder(
    ICodeBuilder<Block> blockBuilder,
    ICodeBuilder<Variable> variableBuilder)
    : ICodeBuilder<IStatement>
{
    public void Build(BuildContext ctx, in IStatement statement)
    {
        var curVariable = statement.Current;
        if (curVariable.Injection.Tag != MdTag.ContextTag)
        {
            ctx = ctx with { ContextTag = curVariable.Injection.Tag };
        }

        switch (statement)
        {
            case Variable variable:
                if (!variable.Info.CreateVariable(variable.ParentBlock))
                {
                    break;
                }

                variableBuilder.Build(ctx, variable);
                break;

            case Block block:
                if (!block.Current.Info.CreateBlock(block.Current.ParentBlock))
                {
                    break;
                }

                blockBuilder.Build(ctx, block);
                break;
        }
    }
}