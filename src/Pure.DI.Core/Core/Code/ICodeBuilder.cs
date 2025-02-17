namespace Pure.DI.Core.Code;

interface ICodeBuilder<T>
{
    void Build(BuildContext ctx, in T target);
}