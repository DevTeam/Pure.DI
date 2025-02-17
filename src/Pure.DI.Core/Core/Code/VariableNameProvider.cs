namespace Pure.DI.Core.Code;

class VariableNameProvider(IIdGenerator idGenerator) : IVariableNameProvider
{
    public string GetVariableName(DependencyNode node, int transientId)
    {
        switch (node)
        {
            case { Lifetime: Lifetime.Singleton }:
            {
                var binding = node.Binding;
                return GetVariableName(Names.SingletonVariablePrefix, node.Type.Name, binding.Id);
            }

            case { Lifetime: Lifetime.Scoped }:
            {
                var binding = node.Binding;
                return GetVariableName(Names.ScopedVariablePrefix, node.Type.Name, binding.Id);
            }

            case { Lifetime: Lifetime.PerResolve }:
                return GetVariableName(Names.PerResolveVariablePrefix, node.Type.Name, transientId);

            case { Arg: { Source.Kind: ArgKind.Class } arg }:
                return $"{Names.ArgVariablePrefix}{ToTitleCase(arg.Source.ArgName)}{Names.Salt}";

            case { Arg: { Source.Kind: ArgKind.Root } arg }:
                return arg.Source.ArgName;

            case { Lifetime: Lifetime.PerBlock }:
                return GetVariableName(Names.PerBlockVariablePrefix, node.Type.Name, transientId);

            default:
                return GetVariableName(Names.TransientVariablePrefix, node.Type.Name, transientId);
        }
    }

    public string GetLocalUniqueVariableName(string baseName) =>
        GetVariableName(Names.LocalVariablePrefix, baseName, idGenerator.Generate());

    private static string GetVariableName(string prefix, string baseName, int id) =>
        $"{prefix}{ToTitleCase(baseName)}{Names.Salt}{id}";

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