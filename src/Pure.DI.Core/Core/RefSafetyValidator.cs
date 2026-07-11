// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core;

using static Lifetime;

sealed class RefSafetyValidator(
    ILogger logger,
    IRefSafety refSafety,
    IInjectionComparer injectionComparer,
    ILocationProvider locationProvider)
    : IValidator<DependencyGraph>
{
    public bool Validate(DependencyGraph dependencyGraph)
    {
        if (!dependencyGraph.IsResolved)
        {
            return false;
        }

        var isValid = true;
        var reported = new HashSet<ReportKey>();
        foreach (var dependency in dependencyGraph.Graph.Edges)
        {
            var targetType = dependency.Target.Type;
            if (!refSafety.ContainsMaybeRefLike(targetType) && !refSafety.ContainsMaybeRefLike(dependency.Injection.Type))
            {
                continue;
            }

            isValid &= ValidateStorageLifetime(dependency, reported);
            isValid &= ValidateDelegateCapture(dependency, reported);
            isValid &= ValidateInjectionSite(dependency, reported);
        }

        foreach (var node in dependencyGraph.Graph.Vertices)
        {
            isValid &= ValidateDelegateFactoryOverrides(node, dependencyGraph.Source.Hints, reported);
            isValid &= ValidateInterfaceConversion(node, reported);
        }

        return isValid ? true : throw HandledException.Shared;
    }

    private bool ValidateStorageLifetime(Dependency dependency, HashSet<ReportKey> reported)
    {
        var consumer = dependency.Target;
        if (consumer.Lifetime is not (Singleton or Scoped or PerResolve)
            || !refSafety.ContainsMaybeRefLikeValue(dependency.Injection.Type))
        {
            return true;
        }

        Report(
            reported,
            LogId.ErrorStackOnlyDependencyWithStoredLifetime,
            nameof(Strings.Description_ErrorStackOnlyDependencyWithStoredLifetime),
            Strings.Description_ErrorStackOnlyDependencyWithStoredLifetime,
            locationProvider.GetLocation(consumer.Binding.Source));
        return false;
    }

    private bool ValidateDelegateCapture(Dependency dependency, HashSet<ReportKey> reported)
    {
        var consumer = dependency.Target;
        if (consumer.Factory is null
            || consumer.Type.TypeKind != TypeKind.Delegate
            || !refSafety.IsMaybeRefLike(dependency.Injection.Type))
        {
            return true;
        }

        Report(
            reported,
            LogId.ErrorStackOnlyDelegateCapture,
            nameof(Strings.Description_ErrorStackOnlyDelegateCapture),
            Strings.Description_ErrorStackOnlyDelegateCapture,
            GetLocation(dependency.Injection));
        return false;
    }

    private bool ValidateDelegateFactoryOverrides(DependencyNode node, IHints hints, HashSet<ReportKey> reported)
    {
        if (node is not { Factory: {} factory, Type.TypeKind: TypeKind.Delegate })
        {
            return true;
        }

        var isValid = true;
        var overrides = factory.Resolvers
            .SelectMany(i => i.Overrides)
            .Concat(factory.Initializers.SelectMany(i => i.Overrides));
        foreach (var @override in overrides)
        {
            if (!refSafety.ContainsMaybeRefLike(@override.Source.ContractType))
            {
                continue;
            }

            if (IsDelegateParameterOverride(@override.Source))
            {
                ValidateDelegateParameterOverrideLock(hints, @override.Source, reported);
                continue;
            }

            Report(
                reported,
                LogId.ErrorStackOnlyDelegateCapture,
                nameof(Strings.Description_ErrorStackOnlyDelegateCapture),
                Strings.Description_ErrorStackOnlyDelegateCapture,
                locationProvider.GetLocation(@override.Source.Source));
            isValid = false;
        }

        return isValid;
    }

    private bool ValidateDelegateParameterOverrideLock(IHints hints, in MdOverride @override, HashSet<ReportKey> reported)
    {
        if (!hints.IsThreadSafeEnabled
            || IsUnderContextLock(@override.Source))
        {
            return true;
        }

        ReportWarning(
            reported,
            LogId.WarningStackOnlyOverrideRequiresLock,
            nameof(Strings.Description_WarningStackOnlyOverrideRequiresLock),
            Strings.Description_WarningStackOnlyOverrideRequiresLock,
            locationProvider.GetLocation(@override.Source));
        return true;
    }

    private static bool IsDelegateParameterOverride(in MdOverride @override)
    {
        if (@override.Source is not InvocationExpressionSyntax invocation)
        {
            return false;
        }

        var argumentList = invocation.ArgumentList;
        if (argumentList is null
            || argumentList.Arguments.Count == 0
            || argumentList.Arguments[0].Expression is not IdentifierNameSyntax identifier)
        {
            return false;
        }

        var lambda = identifier.Ancestors().OfType<LambdaExpressionSyntax>().FirstOrDefault();
        var parameterName = identifier.Identifier.ValueText;
        return lambda switch
        {
            SimpleLambdaExpressionSyntax simpleLambda => simpleLambda.Parameter.Identifier.ValueText == parameterName,
            ParenthesizedLambdaExpressionSyntax parenthesizedLambda => parenthesizedLambda.ParameterList.Parameters.Any(parameter => parameter.Identifier.ValueText == parameterName),
            _ => false
        };
    }

    private static bool IsUnderContextLock(ExpressionSyntax source)
    {
        foreach (var lockStatement in source.Ancestors().OfType<LockStatementSyntax>())
        {
            if (lockStatement.Expression is MemberAccessExpressionSyntax
                {
                    Expression: IdentifierNameSyntax { Identifier.ValueText: "ctx" },
                    Name.Identifier.ValueText: nameof(IContext.Lock)
                })
            {
                return true;
            }
        }

        return false;
    }

    private bool ValidateInjectionSite(Dependency dependency, HashSet<ReportKey> reported)
    {
        var consumer = dependency.Target;
        if (consumer.Implementation is not {} implementation)
        {
            return true;
        }

        var injection = dependency.Injection;
        if (MatchesConstructor(implementation, dependency))
        {
            if (implementation.Source.Type.TypeKind == TypeKind.Class
                && refSafety.ContainsMaybeRefLikeValue(injection.Type))
            {
                ReportWarning(
                    reported,
                    LogId.WarningStackOnlyConstructorInjectionIntoHeapType,
                    nameof(Strings.Description_WarningStackOnlyConstructorInjectionIntoHeapType),
                    Strings.Description_WarningStackOnlyConstructorInjectionIntoHeapType,
                    GetLocation(injection));
            }
        }

        if ((MatchesField(implementation, dependency) || MatchesProperty(implementation, dependency))
            && refSafety.ContainsMaybeRefLikeValue(injection.Type))
        {
            Report(
                reported,
                LogId.ErrorStackOnlyFieldOrPropertyInjection,
                nameof(Strings.Description_ErrorStackOnlyFieldOrPropertyInjection),
                Strings.Description_ErrorStackOnlyFieldOrPropertyInjection,
                GetLocation(injection));
            return false;
        }

        return true;
    }

    private bool ValidateInterfaceConversion(DependencyNode node, HashSet<ReportKey> reported)
    {
        if (node.Implementation is not {} implementation
            || !refSafety.IsRefLike(implementation.Source.Type))
        {
            return true;
        }

        foreach (var contract in implementation.Binding.Contracts)
        {
            if (contract.ContractType is not { TypeKind: TypeKind.Interface })
            {
                continue;
            }

            Report(
                reported,
                LogId.ErrorStackOnlyInterfaceConversion,
                nameof(Strings.Description_ErrorStackOnlyInterfaceConversion),
                Strings.Description_ErrorStackOnlyInterfaceConversion,
                locationProvider.GetLocation(contract.Source));
            return false;
        }

        return true;
    }

    private bool MatchesConstructor(DpImplementation implementation, Dependency dependency) =>
        implementation.Constructor.Parameters.Any(parameter => Matches(parameter.Injection, dependency));

    private bool MatchesField(DpImplementation implementation, Dependency dependency) =>
        implementation.Fields.Any(field => Matches(field.Injection, dependency));

    private bool MatchesProperty(DpImplementation implementation, Dependency dependency) =>
        implementation.Properties.Any(property => Matches(property.Injection, dependency));

    private bool Matches(Injection injection, Dependency dependency) =>
        injectionComparer.Equals(injection, dependency.Injection)
        && dependency.Position is null;

    private Location GetLocation(Injection injection) =>
        injection.Locations.FirstOrDefault() ?? Location.None;

    private void Report(
        HashSet<ReportKey> reported,
        string id,
        string messageKey,
        string message,
        Location location)
    {
        if (!reported.Add(new ReportKey(id, location)))
        {
            return;
        }

        logger.CompileError(
            LogMessage.From(messageKey, message),
            ImmutableArray.Create(location),
            id);
    }

    private void ReportWarning(
        HashSet<ReportKey> reported,
        string id,
        string messageKey,
        string message,
        Location location)
    {
        if (!reported.Add(new ReportKey(id, location)))
        {
            return;
        }

        logger.CompileWarning(
            LogMessage.From(messageKey, message),
            ImmutableArray.Create(location),
            id);
    }

    private readonly record struct ReportKey(string Id, Location Location);
}
