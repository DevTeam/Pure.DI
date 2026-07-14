namespace Pure.DI.Core.Code;

using static Lifetime;

/// <summary>
/// Provides tools for working with variables.
/// </summary>
sealed class VariableTools : IVariableTools
{
    /// <inheritdoc />
    public Comparison<VarInjection> InjectionComparer => (a, b) =>
    {
        // Non-cyclic variables have higher priority than cyclic variables
        var aCyclic = a.Var.HasCycle == true;
        var bCyclic = b.Var.HasCycle == true;
        if (aCyclic != bCyclic)
        {
            return aCyclic ? 1 : -1;
        }

        return GetInjectionPriority(a).CompareTo(GetInjectionPriority(b));
    };

    /// <summary>
    /// Gets the priority of the injection.
    /// The lower the value, the higher the priority.
    /// </summary>
    /// <param name="varInjection">The variable injection.</param>
    /// <returns>The priority value.</returns>
    private static int GetInjectionPriority(VarInjection varInjection)
    {
        var node = varInjection.Var.AbstractNode;
        
        // Arguments have a high priority
        if (node.Arg is not null)
        {
            return 1;
        }

        // Implementation-based nodes priority depends on their lifetime
        if (node.Node.Implementation is not null)
        {
            return node.ActualLifetime switch
            {
                PerBlock => 10,
                Singleton => 11,
                Scoped => 12,
                PerResolve => 13,
                _ => 14
            };
        }

        // Construction-based nodes priority depends on the kind of construction
        if (node.Construct is { } construct)
        {
            return construct.Source.Kind switch
            {
                MdConstructKind.Accumulator => 0,
                MdConstructKind.ExplicitDefaultValue => 1,
                MdConstructKind.Composition => 1,
                MdConstructKind.OnCannotResolve => 20,
                MdConstructKind.Enumerable => 22,
                MdConstructKind.Array => 21,
                MdConstructKind.Span => 21,
                MdConstructKind.SpanConversion => 21,
                MdConstructKind.AsyncEnumerable => 22,
                MdConstructKind.Override => 28,
                _ => 29
            };
        }

        // Factory-based nodes priority depends on the number of resolvers and initializers
        if (node.Node.Factory is {} factory)
        {
            return 30 + factory.Resolvers.Length + factory.Initializers.Length * 100 + (factory.HasOverrides ? 1000 : 0);
        }

        return int.MaxValue;
    }
}
