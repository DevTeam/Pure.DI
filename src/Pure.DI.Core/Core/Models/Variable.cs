// ReSharper disable HeapView.BoxingAllocation
// ReSharper disable PropertyCanBeMadeInitOnly.Global
namespace Pure.DI.Core.Models;

internal sealed record Variable(
    DependencyGraph Source,
    int Id,
    DependencyNode Node,
    in Injection Injection)
{
    internal static readonly string Salt = $"M{DateTime.Now.Month:00}D{DateTime.Now.Day:00}di";
    internal static readonly string DisposeIndexFieldName = "_disposeIndex" + Salt;
    internal static readonly string DisposablesFieldName = "_disposableSingletons" + Salt;
    internal static readonly string InjectionMarker = "injection" + Salt;

    public string Name
    {
        get
        {
            switch (Node)
            {
                case { Lifetime: Lifetime.Singleton }:
                {
                    var binding = Node.Binding;
                    return $"{Constant.SingletonVariablePrefix}{Salt}{binding.Id}";
                }

                case { Lifetime: Lifetime.PerResolve }:
                    return $"{Constant.PerResolveVariablePrefix}{Salt}{Id}";
                
                case { Arg: { Source.Kind: ArgKind.Class} arg }:
                    return $"{Constant.ArgVariablePrefix}{Salt}{arg.Source.ArgName}";
                
                case { Arg: { Source.Kind: ArgKind.Root} arg }:
                    return arg.Source.ArgName;

                default:
                    return $"{Constant.TransientVariablePrefix}{Salt}{Id}";
            }
        }
    }
        
    public ITypeSymbol InstanceType => Node.Type;
    
    public ITypeSymbol ContractType => Injection.Type;

    public bool IsDeclared { get; set; }
    
    public bool IsCreated { get; set; }
    
    public DependencyNode? Owner { get; set; }

    public bool IsCreationRequired(in DependencyNode node) => 
        !IsCreated && (!Owner.HasValue || Owner.Equals(node));

    public void AllowCreation() => Owner = default;
    
    public bool IsBlockRoot { get; init; }
    
    public override string ToString() => $"{ContractType} {Name}";
}