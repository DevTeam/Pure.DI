// ReSharper disable InconsistentNaming
namespace Pure.DI.Core;

internal static class Names
{
    public static readonly string Salt = $"M{DateTime.Now.Month:00}D{DateTime.Now.Day:00}di";
    public static readonly string DisposeIndexFieldName = "_disposeIndex" + Salt;
    public static readonly string DisposablesFieldName = "_disposableSingletons" + Salt;
    public static readonly string InjectionMarker = "injection" + Salt;
    
    public const string GeneratorName = $"{nameof(Pure)}.{nameof(DI)}";
    public const string ApiNamespace = $"global::{GeneratorName}.";
    public const string CannotResolve = "Cannot resolve composition root";
    public const string SystemNamespace = $"global::{nameof(System)}.";
    public const string MethodImplOptions = $"[{SystemNamespace}Runtime.CompilerServices.MethodImpl(({SystemNamespace}Runtime.CompilerServices.MethodImplOptions)0x300)]";
    public const string DefaultApiMethodModifiers = "public";
    public const string ResolverMethodName = "Resolve";
    public const string IDisposableInterfaceName = $"{SystemNamespace}{nameof(IDisposable)}";
    public const string OnNewInstanceMethodName = "OnNewInstance";
    public const string OnDisposeExceptionMethodName = "OnDisposeException";
    public const string OnDependencyInjectionMethodName = "OnDependencyInjection";
    public const string OnCannotResolve = "OnCannotResolve";
    public const string OnNewRootMethodName = "OnNewRoot";
    
    public const string TransientVariablePrefix = "transient";
    public const string PerResolveVariablePrefix = "perResolve";
    public const string SingletonVariablePrefix = "_singleton";
    public const string ArgVariablePrefix = "_arg";
}