// ReSharper disable InconsistentNaming
namespace Pure.DI.Core;

internal static class Constant
{
    public const string ApiNamespace = "global::Pure.DI.";
    public const string CannotResolve = "Cannot resolve composition root";
    public const string SystemNamespace = "global::System.";
    public const string MethodImplOptions = $"[{SystemNamespace}Runtime.CompilerServices.MethodImpl(({SystemNamespace}Runtime.CompilerServices.MethodImplOptions)0x300)]";
    public const string DefaultApiMethodModifiers = "public";
    public const string ResolverMethodName = "Resolve";
    public const string IDisposableInterfaceName = "System.IDisposable";
    public const string OnNewInstanceMethodName = "OnNewInstance";
    public const string OnDependencyInjectionMethodName = "OnDependencyInjection";
    public const string OnCannotResolve = "OnCannotResolve";
}