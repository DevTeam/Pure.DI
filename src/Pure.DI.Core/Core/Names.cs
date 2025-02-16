// ReSharper disable InconsistentNaming

namespace Pure.DI.Core;

using System.Runtime.CompilerServices;

internal static class Names
{
    public static readonly string Salt = $"M{DateTime.Now.Month:00}D{DateTime.Now.Day:00}di";
    public const string GeneratorName = $"{nameof(Pure)}.{nameof(DI)}";
    public static readonly string InjectionMarker = "injection" + Salt;
    public static readonly string InitializationMarker = "initialization" + Salt;

    // Namespaces
    public const string GlobalNamespacePrefix = "global::";
    public const string ApiNamespace = $"{GlobalNamespacePrefix}{GeneratorName}.";
    public const string SystemNamespace = $"{GlobalNamespacePrefix}{nameof(System)}.";

    // Attributes
    public const string MethodImplAttributeName = $"{SystemNamespace}Runtime.CompilerServices.MethodImpl";
    public const string MethodImplOptionsName = $"{SystemNamespace}Runtime.CompilerServices.{nameof(MethodImplOptions)}";
    public const string MethodImplNoInlining = $"{MethodImplOptionsName}.{nameof(MethodImplOptions.NoInlining)}";

    // Messages
    public const string CannotResolveMessage = "Cannot resolve composition root";
    public const string OfTypeMessage = "of type";

    // Others
    public static readonly string ResolverClassName = $"Resolver{Salt}";
    public const string DefaultApiMethodModifiers = "public";
    public const string ParentScopeArgName = "parentScope";
    public const string ResolverPropertyName = "Value";
    public const string DefaultBuilderName= "BuildUp";
    public const string BuildingInstance = "buildingInstance";
    public const string ContextInstance = "ctx";

    // Attributes
    public const string OrdinalAttributeName = $"{ApiNamespace}{nameof(OrdinalAttribute)}";
    public const string BindAttributeName = $"{ApiNamespace}{nameof(BindAttribute)}";

    // Types
    public const string MarkerTypeName = $"{GeneratorName}.TT";
    public const string IDisposableTypeName = $"{SystemNamespace}{nameof(IDisposable)}";
    public const string IAsyncDisposableTypeName = $"{SystemNamespace}IAsyncDisposable";
    public const string IResolverTypeName = $"{ApiNamespace}{nameof(IResolver<object, object>)}";
    public const string IContextTypeName = $"{ApiNamespace}{nameof(IContext)}";
    public const string ObjectTypeName = $"{SystemNamespace}Object";
    public const string ValueTaskTypeName = $"{SystemNamespace}Threading.Tasks.ValueTask";
    public const string LockTypeName = $"{SystemNamespace}Threading.Lock";
    public const string SpanTypeName = $"{SystemNamespace}Span<>";
    public const string ReadOnlySpanTypeName = $"{SystemNamespace}ReadOnlySpan<>";
    public const string IEnumerableTypeName = $"{SystemNamespace}Collections.Generic.IEnumerable<>";
    public const string IAsyncEnumerableTypeName = $"{SystemNamespace}Collections.Generic.IAsyncEnumerable<>";

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
    public const string TransientVariablePrefix = "transient";
    public const string PerBlockVariablePrefix = "perBlock";
    public const string PerResolveVariablePrefix = "perResolve";
    public const string SingletonVariablePrefix = "_singleton";
    public const string ScopedVariablePrefix = "_scoped";
    public const string ArgVariablePrefix = "_arg";
    public const string LocalVariablePrefix = "local";

    public static string GetPropertyName(this Root root) =>
        root.IsPublic ? root.Name : $"Root{Salt}{root.Index}";
}