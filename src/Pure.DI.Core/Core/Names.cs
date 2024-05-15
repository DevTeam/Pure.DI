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
    
    // Attributes
    public const string MethodImplAttribute = $"{SystemNamespace}Runtime.CompilerServices.MethodImpl";
    public const string MethodImplOptions = $"{SystemNamespace}Runtime.CompilerServices.MethodImplOptions";
    
    // Messages
    public const string CannotResolveMessage = "Cannot resolve composition root";
    public const string OfTypeMessage = "of type";
    
    // Others
    public static readonly string ResolverClassName = $"Resolver{Salt}";
    public const string DefaultApiMethodModifiers = "public";
    public const string ParentScopeArgName = "parentScope";
    
    // Interfaces
    public const string ResolverPropertyName = "Value";
    public const string IDisposableInterfaceName = $"{SystemNamespace}{nameof(IDisposable)}";
    public const string IAsyncDisposableInterfaceName = $"{SystemNamespace}IAsyncDisposable";
    public const string IAsyncDisposableInterfaceShortName = $"{nameof(System)}.IAsyncDisposable";
    public const string ResolverInterfaceName = $"{ApiNamespace}{nameof(IResolver<object, object>)}";
    
    // Classes
    public const string ValueTaskName = $"{SystemNamespace}Threading.Tasks.ValueTask";
    
    // Members
    public const string ResolveMethodName = nameof(IResolver<object, object>.Resolve);
    public const string ResolveByTagMethodName = nameof(IResolver<object, object>.ResolveByTag);
    
    // Partial methods
    public const string OnNewInstanceMethodName = "OnNewInstance";
    public const string OnDisposeExceptionMethodName = "OnDisposeException";
    public const string OnDisposeAsyncExceptionMethodName = "OnDisposeAsyncException";
    public const string OnDependencyInjectionMethodName = "OnDependencyInjection";
    public const string OnCannotResolve = "OnCannotResolve";
    public const string OnNewRootMethodName = "OnNewRoot";
    
    // Local methods
    public const string EnsureExistsMethodNamePrefix = "EnsureExistenceOf";
    public const string EnumerateMethodNamePrefix = "EnumerationOf";
    
    // Fields
    public static readonly string BucketsFieldName = $"_buckets{Salt}";
    public static readonly string BucketSizeFieldName = $"_bucketSize{Salt}";
    public static readonly string DisposeIndexFieldName = "_disposeIndex" + Salt;
    public static readonly string DisposablesFieldName = "_disposables" + Salt;
    public static readonly string LockFieldName = "_lock" + Salt;
    public static readonly string RootFieldName = "_root" + Salt;
    public static readonly string CannotResolveFieldName = "CannotResolveMessage" + Salt;
    public static readonly string OfTypeFieldName = "OfTypeMessage" + Salt;
    
    // Vars
    private const string TransientVariablePrefix = "transient";
    private const string PerBlockVariablePrefix = "perBlock";
    private const string PerResolveVariablePrefix = "perResolve";
    private const string SingletonVariablePrefix = "_singleton";
    private const string ScopedVariablePrefix = "_scoped";
    private const string ArgVariablePrefix = "_arg";
    public const string LocalVariablePrefix = "local";
    
    public static string GetVariableName(this DependencyNode Node, int PerLifetimeId)
    {
        var baseName = Node.Type.Name.ToTitleCase();
        switch (Node)
        {
            case { Lifetime: Lifetime.Singleton }:
            {
                var binding = Node.Binding;
                return $"{SingletonVariablePrefix}{baseName}{Salt}{binding.Id}";
            }
            
            case { Lifetime: Lifetime.Scoped }:
            {
                var binding = Node.Binding;
                return $"{ScopedVariablePrefix}{baseName}{Salt}{binding.Id}";
            }

            case { Lifetime: Lifetime.PerResolve }:
                return $"{PerResolveVariablePrefix}{baseName}{Salt}{PerLifetimeId}";

            case { Arg: { Source.Kind: ArgKind.Class } arg }:
                return $"{ArgVariablePrefix}{ToTitleCase(arg.Source.ArgName)}{Salt}";

            case { Arg: { Source.Kind: ArgKind.Root } arg }:
                return arg.Source.ArgName;
            
            case { Lifetime: Lifetime.PerBlock }:
                return $"{PerBlockVariablePrefix}{baseName}{Salt}{PerLifetimeId}";

            default:
                return $"{TransientVariablePrefix}{baseName}{Salt}{PerLifetimeId}";
        }
    }
    
    public static string GetPropertyName(this Root root) =>
        root.IsPublic ? root.Name : $"Root{Salt}{root.Index}";

    public static string ToTitleCase(this string title)
    {
        return new string(title
            .Where(i => i != '@')
            .Select((ch, index) => index == 0 ? char.ToUpper(ch) : ch)
            .ToArray());
    }
}