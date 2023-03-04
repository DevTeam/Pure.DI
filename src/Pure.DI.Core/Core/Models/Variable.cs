namespace Pure.DI.Core.Models;

internal record Variable(int Id, DependencyNode Node)
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
                    return $"_field{binding.Id}Singleton{Postfix}";
                }

                case { Lifetime: Lifetime.PerResolve }:
                    return $"var{Id}PerResolve{Postfix}";
                
                case { Arg: {} arg }:
                    return $"_field{arg.Source.ArgName}Arg{Postfix}";

                default:
                    return $"var{Id}Local{Postfix}";
            }
        }
    }
        
    public string Type => Node.Type.ToString();

    public bool IsDeclared { get; set; }
    
    public bool IsCreated { get; set; }
    
    public bool IsBlockRoot { get; init; }
    
    public override string ToString() => $"{Node.Lifetime} {Node.Type} {Id}";
}