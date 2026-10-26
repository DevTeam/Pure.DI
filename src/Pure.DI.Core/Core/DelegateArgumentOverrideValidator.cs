// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UseCollectionExpression
namespace Pure.DI.Core;

/// <summary>
/// A value passed to a generated <c>Func&lt;...&gt;</c> delegate is a deep override: it rewrites the whole
/// dependency subgraph of the delegate result. A singleton or scoped dependency inside that subgraph is
/// created once, so it silently stores the value of the first call. The same composition may instead build
/// that dependency outside the delegate, where the value is ignored, depending on the shape of the graph.
/// The diagnostic reports the ambiguous case only; explicit <c>ctx.Override(...)</c> calls are documented
/// behavior and are not reported.
/// </summary>
sealed class DelegateArgumentOverrideValidator(
    ILogger logger,
    ILocationProvider locationProvider)
    : IValidator<DependencyGraph>
{
    public bool Validate(DependencyGraph dependencyGraph)
    {
        if (!dependencyGraph.IsResolved)
        {
            return false;
        }

        var delegateArgumentOverrides = GetDelegateArgumentOverrides(dependencyGraph.Graph);
        if (delegateArgumentOverrides.Count == 0)
        {
            return true;
        }

        var reported = new HashSet<(int BindingId, Location Location)>();
        foreach (var dependency in dependencyGraph.Graph.Edges)
        {
            var consumer = dependency.Target;
            if (consumer.ActualLifetime is not (Lifetime.Singleton or Lifetime.Scoped)
                || dependency.Injection.Kind != InjectionKind.Override
                || dependency.Source.Construct is not { Source.State: DpOverride @override }
                || !delegateArgumentOverrides.Contains(@override.Source.Source))
            {
                continue;
            }

            var location = locationProvider.GetLocation(consumer.Binding.Source);
            if (!reported.Add((consumer.BindingId, location)))
            {
                continue;
            }

            logger.CompileWarning(
                LogMessage.From(
                    nameof(Strings.Description_WarningStoredLifetimeFromDelegateArgument),
                    Strings.Description_WarningStoredLifetimeFromDelegateArgument),
                ImmutableArray.Create(location),
                LogId.WarningStoredLifetimeFromDelegateArgument);
        }

        return true;
    }

    /// <summary>
    /// Collects overrides declared by the built-in <c>Func&lt;...&gt;</c> bindings, whose values are the
    /// arguments of the generated delegate.
    /// </summary>
    private static HashSet<SyntaxNode> GetDelegateArgumentOverrides(IGraph<DependencyNode, Dependency> graph)
    {
        var result = new HashSet<SyntaxNode>();
        foreach (var node in graph.Vertices)
        {
            if (node is not { Factory: {} factory }
                || node.Binding.SourceSetup.Kind != CompositionKind.Global)
            {
                continue;
            }

            foreach (var @override in factory.Resolvers
                         .SelectMany(i => i.Overrides)
                         .Concat(factory.Initializers.SelectMany(i => i.Overrides)))
            {
                if (@override.Source.IsDeep && IsDelegateArgument(@override.Source))
                {
                    result.Add(@override.Source.Source);
                }
            }
        }

        return result;
    }

    private static bool IsDelegateArgument(in MdOverride @override)
    {
        if (@override.Source is not InvocationExpressionSyntax invocation
            || invocation.ArgumentList.Arguments is not [{} valueArgument, ..]
            || valueArgument.Expression is not IdentifierNameSyntax identifier)
        {
            return false;
        }

        var parameterName = identifier.Identifier.ValueText;
        var lambda = identifier.Ancestors().OfType<LambdaExpressionSyntax>().FirstOrDefault();
        return lambda switch
        {
            SimpleLambdaExpressionSyntax simpleLambda => simpleLambda.Parameter.Identifier.ValueText == parameterName,
            ParenthesizedLambdaExpressionSyntax parenthesizedLambda => parenthesizedLambda.ParameterList.Parameters.Any(parameter => parameter.Identifier.ValueText == parameterName),
            _ => false
        };
    }
}
