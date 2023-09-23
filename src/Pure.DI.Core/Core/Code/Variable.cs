namespace Pure.DI.Core.Code;

internal record Variable(
        IStatement? Parent,
        int Id,
        in DependencyNode Node,
        in Injection Injection,
        ICollection<IStatement> Args,
        VariableInfo Info)
        : IStatement
{
    public Variable Current => this;
    
    public bool IsDeclared { get; } = Node.Lifetime != Lifetime.Transient || Node.IsArg();
    
    public string VarName
    {
        get
        {
            switch (Node)
            {
                case { Lifetime: Lifetime.Singleton }:
                {
                    var binding = Node.Binding;
                    return $"{Names.SingletonVariablePrefix}{Names.Salt}{binding.Id}";
                }

                case { Lifetime: Lifetime.PerResolve }:
                    return $"{Names.PerResolveVariablePrefix}{Names.Salt}{Id}";

                case { Arg: { Source.Kind: ArgKind.Class } arg }:
                    return $"{Names.ArgVariablePrefix}{Names.Salt}{arg.Source.ArgName}";

                case { Arg: { Source.Kind: ArgKind.Root } arg }:
                    return arg.Source.ArgName;

                default:
                    return $"{Names.TransientVariablePrefix}{Names.Salt}{Id}";
            }
        }
    }
    
    public ITypeSymbol InstanceType => Node.Type;
    
    public ITypeSymbol ContractType => Injection.Type;

    public override string ToString() => $"{InstanceType} {VarName}";
}