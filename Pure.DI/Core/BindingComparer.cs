namespace Pure.DI.Core;

internal sealed class BindingComparer : IEqualityComparer<IBindingMetadata>
{
    public static readonly IEqualityComparer<IBindingMetadata> Shared = new BindingComparer();

    private BindingComparer() { }

    public bool Equals(IBindingMetadata x, IBindingMetadata y) => x.Id == y.Id;

    public int GetHashCode(IBindingMetadata obj) => obj.Id.GetHashCode();
}