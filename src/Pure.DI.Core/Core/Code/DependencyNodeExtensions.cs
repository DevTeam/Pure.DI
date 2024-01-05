namespace Pure.DI.Core.Code;

internal static class DependencyNodeExtensions
{
    public static bool IsArg(this DependencyNode node) => 
        node.Arg is not null;
    
    public static bool IsFactory(this DependencyNode node) => 
        node.Factory is not null;
    
    public static bool IsEnumerable(this DependencyNode node) =>
        node.Construct is { Source.Kind: MdConstructKind.Enumerable };
    
    public static bool IsAsyncEnumerable(this DependencyNode node) =>
        node.Construct is { Source.Kind: MdConstructKind.AsyncEnumerable };

    private static bool IsDelegate(this DependencyNode node) =>
        node.Type.TypeKind == TypeKind.Delegate;
    
    public static bool IsLazy(this DependencyNode node) =>
        node.IsDelegate() || node.IsEnumerable() || node.IsAsyncEnumerable();
    
    public static bool IsDisposable(this DependencyNode node) => 
        node.Type.AllInterfaces.Any(i => i.SpecialType == SpecialType.System_IDisposable);
}