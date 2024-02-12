namespace Pure.DI.Core.Code;

internal interface ICommenter<in TTarget>
{
    void AddComments(CompositionCode composition, TTarget target);
}