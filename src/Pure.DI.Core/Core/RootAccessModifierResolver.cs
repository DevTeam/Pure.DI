// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core;

sealed class RootAccessModifierResolver : IRootAccessModifierResolver
{
    public Accessibility Resolve(Root root) =>
        root.IsPublic switch
        {
            true when (root.Kind & RootKinds.Public) != 0 => Accessibility.Public,
            true when (root.Kind & RootKinds.Internal) != 0 => Accessibility.Internal,
            true when (root.Kind & RootKinds.Protected) != 0 => Accessibility.Protected,
            true when (root.Kind & RootKinds.Private) != 0 => Accessibility.Private,
            false => Accessibility.Private,
            _ => Accessibility.Public
        };
}