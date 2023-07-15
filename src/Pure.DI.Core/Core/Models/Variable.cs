// ReSharper disable HeapView.BoxingAllocation
// ReSharper disable PropertyCanBeMadeInitOnly.Global
namespace Pure.DI.Core.Models;

internal record Variable(
    DependencyGraph Source,
    int Id,
    DependencyNode Node,
    in Injection Injection)
{
    internal static readonly string Salt = $"M{DateTime.Now.Month:00}D{DateTime.Now.Day:00}di";
    internal static readonly string DisposeIndexFieldName = "_disposeIndex" + Salt;
    internal static readonly string DisposablesFieldName = "_disposableSingletons" + Salt;
    internal static readonly string InjectionMarker = "injection" + Salt;

    public Variable CreateLinkedVariable(in Injection injection) => 
        new LinkedVariable(this, injection);

    public string Name
    {
        get
        {
            switch (Node)
            {
                case { Lifetime: Lifetime.Singleton }:
                {
                    var binding = Node.Binding;
                    return $"_singleton{Salt}_{binding.Id:0000}";
                }

                case { Lifetime: Lifetime.PerResolve }:
                    return $"perResolve{Salt}_{Id:0000}";
                
                case { Arg: {} arg }:
                    return $"_arg{Salt}_{arg.Source.ArgName}";

                default:
                    return $"transient{Salt}_{Id:0000}";
            }
        }
    }
        
    public ITypeSymbol InstanceType => Node.Type;
    
    public ITypeSymbol ContractType => Injection.Type;

    public virtual bool IsDeclared { get; set; }
    
    public virtual bool IsCreated { get; set; }
    
    public virtual bool IsBlockRoot { get; init; }
    
    public override string ToString() => Name;
    
    private record LinkedVariable : Variable
    {
        private readonly Variable _variable;

        public LinkedVariable(Variable variable, in Injection injection)
            : base(variable.Source, variable.Id, variable.Node, injection)
        {
            _variable = variable;
        }

        public override bool IsDeclared
        {
            get => _variable.IsDeclared;
            set => _variable.IsCreated = value;
        }

        public override bool IsCreated
        {
            get => _variable.IsCreated;
            set => _variable.IsCreated = value;
        }

        public override bool IsBlockRoot => _variable.IsBlockRoot;

        public override string ToString() => _variable.ToString();
    }
}