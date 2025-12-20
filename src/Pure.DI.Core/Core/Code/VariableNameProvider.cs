namespace Pure.DI.Core.Code;

class NameProvider(IUniqueNameProvider uniqueNameProvider): INameProvider
{
    public string GetVariableName(IDependencyNode node, int transientId) =>
        node switch
        {
            { Construct.Source: { Kind: MdConstructKind.Override, State: DpOverride @override } } => GetOverrideVariableName(@override.Source),
            { ActualLifetime: Lifetime.Singleton } => GetVariableName(Names.SingletonVariablePrefix, node.Node.Type.Name, node.BindingId),
            { ActualLifetime: Lifetime.Scoped } => GetVariableName(Names.ScopedVariablePrefix, node.Node.Type.Name, node.BindingId),
            { ActualLifetime: Lifetime.PerResolve } => GetVariableName(Names.PerResolveVariablePrefix, node.Node.Type.Name, transientId),
            { Arg: { Source.Kind: ArgKind.Composition } arg } => $"{Names.ArgVariablePrefix}{ToTitleCase(arg.Source.ArgName)}{Names.Salt}",
            { Arg: { Source.Kind: ArgKind.Root } arg } => arg.Source.ArgName,
            { ActualLifetime: Lifetime.PerBlock } => GetVariableName(Names.PerBlockVariablePrefix, node.Node.Type.Name, transientId),
            _ => GetVariableName(Names.TransientVariablePrefix, node.Node.Type.Name, transientId)
        };

    public string GetOverrideVariableName(MdOverride @override) =>
        GetVariableName(Names.OverriddenVariablePrefix, @override.ContractType.Name, @override.Id);

    public string GetLocalUniqueVariableName(string baseName) =>
        uniqueNameProvider.GetUniqueName($"{Names.LocalVariablePrefix}{ToTitleCase(baseName)}{Names.Salt}");

    private static string GetVariableName(string prefix, string baseName, int id) =>
        $"{prefix}{ToTitleCase(baseName)}{Names.Salt}{(id != 0 ? id.ToString() : "")}";

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