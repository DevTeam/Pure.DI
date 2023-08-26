namespace Pure.DI.Core.Code;

internal interface ICodeBuilder<T>
{
    void Build(BuildContext ctx, in T target);
}