namespace Pure.DI.Core.Code;

interface IVarsMap : IDictionary<MdBinding, Variable>
{
    bool IsThreadSafe { get; }

    void Reset();

    IEnumerable<Variable> GetSingletons();

    IEnumerable<Variable> GetPerResolves();
}