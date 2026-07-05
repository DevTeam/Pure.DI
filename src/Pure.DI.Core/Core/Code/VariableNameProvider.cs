namespace Pure.DI.Core.Code;

using System.Collections.Concurrent;

class NameProvider(IUniqueNameProvider uniqueNameProvider): INameProvider
{
    private readonly ConcurrentDictionary<int, string> _overrideVariableNames = new();
    private readonly ConcurrentDictionary<int, string> _persistentVariableNames = new();
    private Dictionary<string, int>? _rootVariableNames;

    public IDisposable Root()
    {
        var rootVariableNames = _rootVariableNames;
        _rootVariableNames = new Dictionary<string, int>(StringComparer.Ordinal);
        _overrideVariableNames.Clear();
        return Disposables.Create(() => {
            _rootVariableNames = rootVariableNames;
            _overrideVariableNames.Clear();
        });
    }

    public string GetVariableName(IDependencyNode node) =>
        node switch
        {
            { Construct.Source: { Kind: MdConstructKind.Override, State: DpOverride @override } } => GetOverrideVariableName(@override.Source),
            { ActualLifetime: Lifetime.Singleton } => GetPersistentVariableName(Names.SingletonVariablePrefix, node.Node.Type.Name, node.BindingId),
            { ActualLifetime: Lifetime.Scoped } => GetPersistentVariableName(Names.ScopedVariablePrefix, node.Node.Type.Name, node.BindingId),
            { ActualLifetime: Lifetime.PerResolve } => GetUniqueVariableName(Names.PerResolveVariablePrefix, GetTypeName(node.Node.Type)),
            { Arg: { Source.Kind: ArgKind.Composition } arg } => arg.Source.IsSetupContext
                ? arg.Source.ArgName
                : $"{Names.ArgVariablePrefix}{ToTitleCase(arg.Source.ArgName)}{Names.Salt}",
            { Arg: { Source.Kind: ArgKind.Root } arg } => arg.Source.ArgName,
            { ActualLifetime: Lifetime.PerBlock } => GetUniqueVariableName(Names.PerBlockVariablePrefix, GetTypeName(node.Node.Type)),
            _ => GetUniqueVariableName(Names.TransientVariablePrefix, GetTypeName(node.Node.Type))
        };

    public string GetOverrideVariableName(MdOverride @override) =>
        _overrideVariableNames.GetOrAdd(@override.Id, _ => GetUniqueVariableName(Names.OverriddenVariablePrefix, GetTypeName(@override.ContractType)));

    public string GetLocalUniqueVariableName(string baseName) =>
        GetRootUniqueName($"{Names.LocalVariablePrefix}{ToTitleCase(baseName)}");

    public string GetUniqueRootName(string name, ITypeSymbol rootType) =>
        uniqueNameProvider.GetUniqueName(string.IsNullOrWhiteSpace(name) ? ToTitleCase(rootType.Name) : name);

    private string GetPersistentVariableName(string prefix, string baseName, int id) =>
        _persistentVariableNames.GetOrAdd(id, _ => uniqueNameProvider.GetUniqueName($"{prefix}{ToTitleCase(baseName)}{Names.Salt}"));

    private string GetUniqueVariableName(string prefix, string baseName) =>
        GetRootUniqueName($"{prefix}{ToTitleCase(baseName)}");

    private string GetRootUniqueName(string baseName)
    {
        if (_rootVariableNames is null)
        {
            return uniqueNameProvider.GetUniqueName(baseName);
        }

        _rootVariableNames.TryGetValue(baseName, out var id);
        _rootVariableNames[baseName] = id + 1;
        return id == 0 ? baseName : $"{baseName}{id}";
    }

    private static string GetTypeName(ITypeSymbol type) =>
        type is INamedTypeSymbol { IsGenericType: true } namedType
            ? namedType.Name + string.Concat(namedType.TypeArguments.Select(GetTypeName))
            : type.Name;

    private static string ToTitleCase(string title)
    {
        if (title.Length == 0)
        {
            return title;
        }

        var firstChar = title[0];
        if (firstChar == '@')
        {
            // ReSharper disable once TailRecursiveCall
            return ToTitleCase(title[1..]);
        }

        // ReSharper disable once InvertIf
        if (char.IsLower(firstChar))
        {
            var chars = title.ToArray();
            chars[0] = char.ToUpper(firstChar);
            return new string(chars);
        }

        return title;
    }
}
