namespace Pure.DI.Core.Code;

interface IVarStateTracker
{
    void OnStateChanging(int bindingId);
}
