namespace Pure.DI.Core;

interface IBindingBuilder
{
    MdLifetime Lifetime { set; }

    MdImplementation Implementation { set; }

    MdFactory Factory { set; }

    MdArg Arg { set; }

    void AddDefaultLifetime(MdDefaultLifetime defaultLifetime);

    void AddContract(in MdContract contract);

    void AddTag(in MdTag tag);

    void CopyConfigurationTo(IBindingBuilder target);

    MdBinding Build(MdSetup setup);
}
