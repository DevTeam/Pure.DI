namespace Pure.DI.Core.Code;

interface IVariablesMap : IDictionary<MdBinding, Variable>
{
    bool IsThreadSafe { get; }

    void Reset();

    IEnumerable<Variable> GetSingletons();


    IEnumerable<Variable> GetPerResolves();
}