// ReSharper disable InconsistentNaming

namespace Pure.DI.Core;

using System.Runtime.CompilerServices;

static class Names
{
    public static readonly string Salt = $"{DateTime.Now.DayOfYear}d";
    public const string GeneratorName = $"{nameof(Pure)}.{nameof(DI)}";

    // Files
    public const string CodeFileSuffix = ".g.cs";

    // Namespaces
    public const string GlobalNamespacePrefix = "global::";
    public const string ApiNamespace = $"{GlobalNamespacePrefix}{GeneratorName}.";
    public const string SystemNamespace = $"{GlobalNamespacePrefix}{nameof(System)}.";

    // Attributes
    public const string MethodImplAttributeName = $"{SystemNamespace}Runtime.CompilerServices.MethodImpl";
    public const string MethodImplOptionsName = $"{SystemNamespace}Runtime.CompilerServices.{nameof(MethodImplOptions)}";

    // Messages
    public const string CannotResolveMessage = "Cannot resolve composition root";
    public const string OfTypeMessage = "of type";
    public const string CannotBuildMessage = "Unable to build an instance of type" + OfTypeMessage;

    public const string DefaultApiMethodModifiers = "public";
    public const string ParentScopeArgName = "parentScope";
    public const string ResolverPropertyName = "Value";
    public const string DefaultBuilderName = "BuildUp";
    public const string BuildingInstance = "buildingInstance";
    public const string ContextInstance = "ctx";
    public const string DefaultInstanceValueName = "instance_1182D127";
    public static readonly string PrivateRootName = $"Root{Salt}";
    public const string TempInstanceValueNameSuffix = "Temp";
    public const string CreatedValueNameSuffix = "Created";

    // Attributes
    public const string OrdinalAttributeName = $"{ApiNamespace}{nameof(OrdinalAttribute)}";
    public const string BindAttributeName = $"{ApiNamespace}{nameof(BindAttribute)}";

    // Types
    public const string IDisposableTypeName = $"{SystemNamespace}{nameof(IDisposable)}";
    public const string IAsyncDisposableTypeName = $"{SystemNamespace}IAsyncDisposable";
    public const string IResolverTypeName = $"{ApiNamespace}{nameof(IResolver<,>)}";
    public const string IContextTypeName = $"{ApiNamespace}{nameof(IContext)}";
    public const string IConfigurationTypeName = $"{ApiNamespace}{nameof(IConfiguration)}";
    public const string ObjectTypeName = $"{SystemNamespace}Object";
    public const string ExceptionTypeName = $"{SystemNamespace}{nameof(Exception)}";
    public const string ArgumentExceptionTypeName = $"{SystemNamespace}{nameof(ArgumentException)}";
    public const string ValueTaskTypeName = $"{SystemNamespace}Threading.Tasks.ValueTask";
    public const string LockTypeName = $"{SystemNamespace}Threading.Lock";
    public const string SpanTypeName = $"{SystemNamespace}Span<>";
    public const string ReadOnlySpanTypeName = $"{SystemNamespace}ReadOnlySpan<>";
    public const string IEnumerableTypeName = $"{SystemNamespace}Collections.Generic.IEnumerable<>";
    public const string IAsyncEnumerableTypeName = $"{SystemNamespace}Collections.Generic.IAsyncEnumerable<>";
    public const string CannotResolveExceptionTypeName = $"{ApiNamespace}{nameof(CannotResolveException)}";
    public const string NonSerializedAttributeTypeName = $"{SystemNamespace}{nameof(NonSerializedAttribute)}";

    // Members
    public const string ResolveMethodName = nameof(IResolver<,>.Resolve);
    public const string ResolveByTagMethodName = nameof(IResolver<,>.ResolveByTag);

    // Partial methods
    public const string OnNewInstanceMethodName = "OnNewInstance";
    public const string OnDisposeExceptionMethodName = "OnDisposeException";
    public const string OnDisposeAsyncExceptionMethodName = "OnDisposeAsyncException";
    public const string OnDependencyInjectionMethodName = "OnDependencyInjection";
    public const string OnCannotResolve = "OnCannotResolve";
    public const string OnNewRootMethodName = "OnNewRoot";

    // Local methods
    public const string EnumerateMethodNamePrefix = "EnumerationOf";

    // Vars
    public const string TransientVariablePrefix = "transient";
    public const string PerBlockVariablePrefix = "perBlock";
    public const string PerResolveVariablePrefix = "perResolve";
    public const string SingletonVariablePrefix = "_singleton";
    public const string ScopedVariablePrefix = "_scoped";
    public const string ArgVariablePrefix = "_arg";
    public const string LocalVariablePrefix = "local";
    public const string OverriddenVariablePrefix = "overridden";
    public static readonly string InjectionMarker = "injectMarker" + Salt;
    public static readonly string InitializationMarker = "initializeMarker" + Salt;
    public static readonly string OverrideMarker = "overrideMarker" + Salt;

    // Others
    public static readonly string ResolverClassName = $"Resolver{Salt}";

    // Fields
    public static readonly string BucketsFieldName = $"_buckets{Salt}";
    public static readonly string BucketSizeFieldName = $"_bucketSize{Salt}";
    public static readonly string DisposeIndexFieldName = "_disposeIndex" + Salt;
    public static readonly string DisposablesFieldName = "_disposables" + Salt;
    public static readonly string LockFieldName = "_lock" + Salt;
    public static readonly string PerResolveLockFieldName = "perResolveLock" + Salt;
    public static readonly string RootFieldName = "_root" + Salt;
    public static readonly string CannotResolveFieldName = "CannotResolveMessage" + Salt;
    public static readonly string OfTypeFieldName = "OfTypeMessage" + Salt;
}