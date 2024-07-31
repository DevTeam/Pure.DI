namespace Pure.DI.Core;

internal interface IBindingBuilder
{
    void AddDefaultLifetime(MdDefaultLifetime defaultLifetime);
    
    MdLifetime Lifetime { set; }
    
    MdImplementation Implementation { set; }
    
    MdFactory Factory { set; }
    
    MdArg Arg { set; }
    
    void AddContract(in MdContract contract);
    
    void AddTag(in MdTag tag);
    
    MdBinding Build(MdSetup setup);
}