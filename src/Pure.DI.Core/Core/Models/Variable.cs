// ReSharper disable HeapView.BoxingAllocation
namespace Pure.DI.Core.Models;

internal record Variable(
    DependencyGraph Source,
    int Id,
    DependencyNode Node,
    Injection Injection)
{
    internal static readonly string Postfix = Guid.NewGuid().ToString().ToUpperInvariant().Replace("-", "")[..6];
    internal static readonly string DisposeIndexFieldName = "_disposeIndex" + Postfix;
    internal static readonly string DisposablesFieldName = "_disposables" + Postfix;

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

    public bool IsDeclared { get; set; }
    
    public bool IsCreated { get; set; }
    
    public bool IsBlockRoot { get; init; }
    
    public override string ToString() => $"{Node.Lifetime} {Node.Type} {Id.ToString()}";
}