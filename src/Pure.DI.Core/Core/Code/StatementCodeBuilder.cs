// ReSharper disable RedundantJumpStatement
// ReSharper disable InvertIf
// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core.Code;

internal class StatementCodeBuilder(
    ICodeBuilder<Block> blockBuilder,
    ICodeBuilder<Variable> variableBuilder)
    : ICodeBuilder<IStatement>
{
    public void Build(BuildContext ctx, in IStatement statement)
    {
        if (statement.Current.Injection.Tag != MdTag.ContextTag)
        {
            ctx = ctx with { ContextTag = statement.Current.Injection.Tag };
        }

        switch (statement)
        {
            case Variable variable:
                if (variable.Info.IsCreated)
                {
                    break;
                }

                variableBuilder.Build(ctx, variable);
                variable.Info.IsCreated = true;
                break;

            case Block block:
                blockBuilder.Build(ctx, block);
                break;
        }
    }
}