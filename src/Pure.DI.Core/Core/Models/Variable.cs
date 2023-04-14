// ReSharper disable HeapView.BoxingAllocation
// ReSharper disable PropertyCanBeMadeInitOnly.Global
namespace Pure.DI.Core.Models;

internal record Variable(
    DependencyGraph Source,
    int Id,
    DependencyNode Node,
    in Injection Injection)
{
    internal static readonly string Postfix = Guid.NewGuid().ToString().ToUpperInvariant().Replace("-", "")[..6];
    internal static readonly string DisposeIndexFieldName = "_disposeIndex" + Postfix;
    internal static readonly string DisposablesFieldName = "_disposables" + Postfix;
    internal static readonly string InjectionMarker = "injection" + Postfix;

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
                    return $"_f{binding.Id.ToString()}Singleton{Postfix}";
                }

                case { Lifetime: Lifetime.PerResolve }:
                    return $"v{Id.ToString()}PerResolve{Postfix}";
                
                case { Arg: {} arg }:
                    return $"_{arg.Source.ArgName}Arg{Postfix}";

                default:
                    return $"v{Id.ToString()}Local{Postfix}";
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