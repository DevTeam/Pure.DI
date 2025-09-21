namespace Pure.DI.Core.Code;

interface IDependencyNode
{
    int BindingId { get; }

    MdBinding Binding { get; }

    Lifetime Lifetime { get; }

    Lifetime ActualLifetime { get; }

    DpArg? Arg { get; }

    DpConstruct? Construct { get; }

    DependencyNode Node { get; }
}