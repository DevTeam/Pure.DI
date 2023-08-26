namespace Pure.DI.Core.Code;

internal static class DependencyNodeExtensions
{
    public static bool IsArg(this in DependencyNode node) => 
        node.Arg is not null;
    
    public static bool IsFactory(this in DependencyNode node) => 
        node.Factory is not null;
    
    public static bool IsEnumerable(this in DependencyNode node) =>
        node.Construct is { Source.Kind: MdConstructKind.Enumerable };

    private static bool IsDelegate(this in DependencyNode node) =>
        node.Type.TypeKind == TypeKind.Delegate;
    
    public static bool IsLazy(this in DependencyNode node) =>
        node.IsDelegate() || node.IsEnumerable();
}