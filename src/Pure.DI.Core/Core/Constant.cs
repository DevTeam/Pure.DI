// ReSharper disable InconsistentNaming
namespace Pure.DI.Core;

internal static class Constant
{
    public const string ApiNamespace = "Pure.DI.";
    public const string CannotResolve = "Cannot resolve composition root";
    public const string MethodImplOptions = "[System.Runtime.CompilerServices.MethodImpl((System.Runtime.CompilerServices.MethodImplOptions)0x300)]";
    public const string ResolverMethodName = "Resolve";
    public const string IDisposableInterfaceName = "System.IDisposable";
    public const string OnInstanceCreationMethodName = "OnInstanceCreation";
    public const string OnDependencyInjectionMethodName = "OnDependencyInjection";
    public const string OnCannotResolve = "OnCannotResolve";
}