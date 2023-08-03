// ReSharper disable InconsistentNaming
namespace Pure.DI.Core;

internal static class Constant
{
    public const string GeneratorName = "Pure.DI";
    public const string ApiNamespace = $"global::{GeneratorName}.";
    public const string CannotResolve = "Cannot resolve composition root";
    public const string SystemNamespace = "global::System.";
    public const string MethodImplOptions = $"[{SystemNamespace}Runtime.CompilerServices.MethodImpl(({SystemNamespace}Runtime.CompilerServices.MethodImplOptions)0x300)]";
    public const string DefaultApiMethodModifiers = "public";
    public const string ResolverMethodName = "Resolve";
    public const string IDisposableInterfaceName = "System.IDisposable";
    public const string OnNewInstanceMethodName = "OnNewInstance";
    public const string OnDependencyInjectionMethodName = "OnDependencyInjection";
    public const string OnCannotResolve = "OnCannotResolve";
    public const string OnNewRootMethodName = "OnNewRoot";
    
    public const string SingletonVariablePrefix = "_singleton";
    public const string PerResolveVariablePrefix = "perResolve";
    public const string ArgVariablePrefix = "_arg";
    public const string TransientVariablePrefix = "transient";
}