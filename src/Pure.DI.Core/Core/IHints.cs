namespace Pure.DI.Core;

interface IHints : IReadOnlyDictionary<Hint, LinkedList<string>>
{
    bool IsCommentsEnabled { get; }

    bool IsOnDependencyInjectionEnabled { get; }

    bool IsOnDependencyInjectionPartial { get; }

    bool IsOnCannotResolveEnabled { get; }

    bool IsOnCannotResolvePartial { get; }

    bool IsOnNewInstanceEnabled { get; }

    bool IsOnNewInstancePartial { get; }

    bool IsOnNewRootEnabled { get; }

    bool IsOnNewRootPartial { get; }

    bool IsThreadSafeEnabled { get; }

    bool IsToStringEnabled { get; }

    bool IsFormatCodeEnabled { get; }

    bool IsResolveEnabled { get; }

    string ResolveMethodName { get; }

    string ResolveMethodModifiers { get; }

    string ResolveByTagMethodName { get; }

    string ResolveByTagMethodModifiers { get; }

    string ObjectResolveMethodName { get; }

    string ObjectResolveMethodModifiers { get; }

    string ObjectResolveByTagMethodName { get; }

    string ObjectResolveByTagMethodModifiers { get; }

    string DisposeMethodModifiers { get; }

    string DisposeAsyncMethodModifiers { get; }

    DiagnosticSeverity SeverityOfNotImplementedContract { get; }

    bool SkipDefaultConstructor { get; }
}