// ReSharper disable InconsistentNaming
namespace Pure.DI.Core;

internal static class Names
{
    public static readonly string Salt = $"M{DateTime.Now.Month:00}D{DateTime.Now.Day:00}di";
    public const string GeneratorName = $"{nameof(Pure)}.{nameof(DI)}";
    public static readonly string InjectionMarker = "injection" + Salt;
    
    // Namespaces
    public const string ApiNamespace = $"global::{GeneratorName}.";
    public const string SystemNamespace = $"global::{nameof(System)}.";
    
    // Messages
    public const string CannotResolve = "Cannot resolve composition root";
    
    // Others
    public static readonly string ResolverClassName = $"Resolver{Salt}";
    public const string DefaultApiMethodModifiers = "public";
    
    // Interfaces
    public const string ResolverPropertyName = "Value";
    public const string IDisposableInterfaceName = $"{SystemNamespace}{nameof(IDisposable)}";
    public const string ResolverInterfaceName = $"{ApiNamespace}{nameof(IResolver<object, object>)}";
    
    // Members
    public const string ResolveMethodName = nameof(IResolver<object, object>.Resolve);
    public const string ResolveByTagMethodName = nameof(IResolver<object, object>.ResolveByTag);
    
    // Partial methods
    public const string OnNewInstanceMethodName = "OnNewInstance";
    public const string OnDisposeExceptionMethodName = "OnDisposeException";
    public const string OnDependencyInjectionMethodName = "OnDependencyInjection";
    public const string OnCannotResolve = "OnCannotResolve";
    public const string OnNewRootMethodName = "OnNewRoot";
    
    // Fields
    public static readonly string BucketsFieldName = $"_buckets{Salt}";
    public static readonly string BucketSizeFieldName = $"_bucketSize{Salt}";
    public static readonly string DisposeIndexFieldName = "_disposeIndex" + Salt;
    public static readonly string DisposablesFieldName = "_disposableSingletons" + Salt;
    
    // Vars
    private const string TransientVariablePrefix = "transient";
    private const string PerBlockVariablePrefix = "perBlock";
    private const string PerResolveVariablePrefix = "perResolve";
    private const string SingletonVariablePrefix = "_singleton";
    private const string ArgVariablePrefix = "_arg";
    
    public static string GetVariableName(this in DependencyNode Node, int PerLifetimeId)
    {
        var baseName = Node.Type.Name;
        switch (Node)
        {
            case { Lifetime: Lifetime.Singleton }:
            {
                var binding = Node.Binding;
                return $"{SingletonVariablePrefix}{Salt}{binding.Id}_{baseName}";
            }

            case { Lifetime: Lifetime.PerResolve }:
                return $"{PerResolveVariablePrefix}{Salt}{PerLifetimeId}_{baseName}";

            case { Arg: { Source.Kind: ArgKind.Class } arg }:
                return $"{ArgVariablePrefix}{Salt}_{arg.Source.ArgName}";

            case { Arg: { Source.Kind: ArgKind.Root } arg }:
                return arg.Source.ArgName;
            
            case { Lifetime: Lifetime.PerBlock }:
                return $"{PerBlockVariablePrefix}{Salt}{PerLifetimeId}_{baseName}";

            default:
                return $"{TransientVariablePrefix}{Salt}{PerLifetimeId}_{baseName}";
        }
    }
    
    public static string GetPropertyName(this Root root) =>
        root.IsPublic ? root.Name : $"Root{Salt}{root.Index:0000}";
}