namespace Pure.DI.Core;

internal sealed class Hints : Dictionary<Hint, string>, IHints
{
    public bool IsCommentsEnabled =>
        IsEnabled(Hint.Comments, SettingState.On);
    
    public bool IsOnDependencyInjectionEnabled =>
        IsEnabled(Hint.OnDependencyInjection, SettingState.Off);

    public bool IsOnDependencyInjectionPartial =>
        IsEnabled(Hint.OnDependencyInjectionPartial, SettingState.On);

    public bool IsOnCannotResolveEnabled =>
        IsEnabled(Hint.OnCannotResolve, SettingState.Off);

    public bool IsOnCannotResolvePartial =>
        IsEnabled(Hint.OnCannotResolvePartial, SettingState.On);

    public bool IsOnNewInstanceEnabled =>
        IsEnabled(Hint.OnNewInstance, SettingState.Off);

    public bool IsOnNewInstancePartial =>
        IsEnabled(Hint.OnNewInstancePartial, SettingState.On);

    public bool IsOnNewRootEnabled =>
        IsEnabled(Hint.OnNewRoot, SettingState.Off);
    
    public bool IsOnNewRootPartial =>
        IsEnabled(Hint.OnNewRootPartial, SettingState.On);

    public bool IsThreadSafeEnabled =>
        IsEnabled(Hint.ThreadSafe, SettingState.On);
    
    public bool IsToStringEnabled =>
        IsEnabled(Hint.ToString, SettingState.Off);

    public bool IsFormatCodeEnabled =>
        IsEnabled(Hint.FormatCode, SettingState.Off);

    public bool IsResolveEnabled =>
        IsEnabled(Hint.Resolve, SettingState.On);

    public string ResolveMethodName =>
        GetValueOrDefault(Hint.ResolveMethodName, Names.ResolveMethodName);

    public string ResolveMethodModifiers =>
        GetValueOrDefault(Hint.ResolveMethodModifiers, Names.DefaultApiMethodModifiers);

    public string ResolveByTagMethodName =>
        GetValueOrDefault(Hint.ResolveByTagMethodName, Names.ResolveMethodName);

    public string ResolveByTagMethodModifiers =>
        GetValueOrDefault(Hint.ResolveByTagMethodModifiers, Names.DefaultApiMethodModifiers);

    public string ObjectResolveMethodName =>
        GetValueOrDefault(Hint.ObjectResolveMethodName, Names.ResolveMethodName);

    public string ObjectResolveMethodModifiers =>
        GetValueOrDefault(Hint.ObjectResolveMethodModifiers, Names.DefaultApiMethodModifiers);

    public string ObjectResolveByTagMethodName =>
        GetValueOrDefault(Hint.ObjectResolveByTagMethodName, Names.ResolveMethodName);

    public string ObjectResolveByTagMethodModifiers =>
        GetValueOrDefault(Hint.ObjectResolveByTagMethodModifiers, Names.DefaultApiMethodModifiers);

    public string DisposeMethodModifiers =>
        GetValueOrDefault(Hint.DisposeMethodModifiers, Names.DefaultApiMethodModifiers);
    
    public string DisposeAsyncMethodModifiers =>
        GetValueOrDefault(Hint.DisposeAsyncMethodModifiers, Names.DefaultApiMethodModifiers);

    public DiagnosticSeverity SeverityOfNotImplementedContract =>
        GetHint(Hint.SeverityOfNotImplementedContract, DiagnosticSeverity.Error);
    
    private bool IsEnabled(Hint hint, SettingState defaultValue) => 
        GetHint(hint, defaultValue) == SettingState.On;

    private T GetHint<T>(Hint hint, T defaultValue = default) 
        where T : struct =>
        TryGetValue(hint, out var valueStr) && Enum.TryParse<T>(valueStr, true, out var value)
            ? value
            : defaultValue;
    
    private string GetValueOrDefault(Hint hint, string defaultValue) =>
        TryGetValue(hint, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : defaultValue;
}