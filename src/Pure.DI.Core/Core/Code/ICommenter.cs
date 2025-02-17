namespace Pure.DI.Core.Code;

interface ICommenter<in TTarget>
{
    void AddComments(CompositionCode composition, TTarget target);
}