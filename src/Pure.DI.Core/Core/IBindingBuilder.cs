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

    MdBinding Build(MdSetup setup);
}