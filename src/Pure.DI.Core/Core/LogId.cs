namespace Pure.DI.Core;

static class LogId
{
    // Errors
    // Unable to resolve a dependency for a root.
    public const string ErrorUnableToResolve = "DIE000";
    // Binding cannot be built due to missing binding metadata.
    public const string ErrorInvalidBinding = "DIE001";
    // Binding is invalid because the compiler reported errors.
    public const string ErrorInvalidBindingDueToCompilation = "DIE002";
    // Composition type name is invalid.
    public const string ErrorInvalidCompositionTypeName = "DIE003";
    // Root name is invalid.
    public const string ErrorInvalidRootName = "DIE004";
    // Duplicate root names detected.
    public const string ErrorDuplicateRootName = "DIE005";
    // Argument name is invalid.
    public const string ErrorInvalidArgumentName = "DIE006";
    // Composition argument type is based on a generic marker.
    public const string ErrorCompositionArgGenericMarker = "DIE007";
    // Accumulator type is based on a generic marker.
    public const string ErrorAccumulatorTypeGenericMarker = "DIE008";
    // Accumulator cannot accumulate a generic marker.
    public const string ErrorAccumulatorCannotAccumulateGenericMarker = "DIE009";
    // Special type cannot be a generic marker.
    public const string ErrorSpecialTypeGenericMarker = "DIE010";
    // Implementation does not implement the contract.
    public const string ErrorNotImplementedContract = "DIE011";
    // Attribute argument position is invalid.
    public const string ErrorInvalidAttributeArgumentPosition = "DIE012";
    // Attribute member cannot be processed.
    public const string ErrorAttributeMemberCannotBeProcessed = "DIE013";
    // Root type is invalid.
    public const string ErrorInvalidRootType = "DIE014";
    // Roots type is invalid.
    public const string ErrorInvalidRootsType = "DIE015";
    // No type matched the wildcard filter.
    public const string ErrorNoTypeForWildcard = "DIE016";
    // Builders type is invalid.
    public const string ErrorInvalidBuildersType = "DIE017";
    // Builder type is invalid.
    public const string ErrorInvalidBuilderType = "DIE018";
    // Too many type parameters provided.
    public const string ErrorTooManyTypeParameters = "DIE019";
    // Async factories are not supported.
    public const string ErrorAsyncFactoryNotSupported = "DIE020";
    // Unsupported syntax in DI API.
    public const string ErrorNotSupportedSyntax = "DIE021";
    // Value must be of a specific type.
    public const string ErrorMustBeValueOfType = "DIE022";
    // Identifier is invalid.
    public const string ErrorInvalidIdentifier = "DIE023";
    // Context is used directly where it is not allowed.
    public const string ErrorCannotUseContextDirectly = "DIE024";
    // Initializers count does not match.
    public const string ErrorInvalidNumberOfInitializers = "DIE025";
    // Lifetime does not support cyclic dependencies.
    public const string ErrorLifetimeDoesNotSupportCyclicDependencies = "DIE026";
    // Composition is too large to build.
    public const string ErrorTooLargeComposition = "DIE027";
    // Cannot construct an abstract type.
    public const string ErrorCannotConstructAbstractType = "DIE028";
    // No accessible constructor found.
    public const string ErrorNoAccessibleConstructor = "DIE029";
    // Dependency graph cannot be built.
    public const string ErrorCannotBuildDependencyGraph = "DIE030";
    // Maximum number of iterations reached.
    public const string ErrorMaximumNumberOfIterations = "DIE031";
    // No accessible constructor found for Tag.On.
    public const string ErrorNoAccessibleConstructorForTagOn = "DIE032";
    // No accessible method found for Tag.On.
    public const string ErrorNoAccessibleMethodForTagOn = "DIE033";
    // No accessible field/property found for Tag.On.
    public const string ErrorNoAccessibleFieldOrPropertyForTagOn = "DIE034";
    // Expression must be a valid API call.
    public const string ErrorMustBeApiCall = "DIE035";
    // Regular expression is invalid.
    public const string ErrorInvalidRegularExpression = "DIE036";
    // Wildcard is invalid.
    public const string ErrorInvalidWildcard = "DIE037";
    // DI setup could not be found.
    public const string ErrorCannotFindSetup = "DIE038";
    // Cyclic dependency detected.
    public const string ErrorCyclicDependency = "DIE039";
    // Language version is not supported.
    public const string ErrorNotSupportedLanguageVersion = "DIE040";
    // Lifetime validation error.
    public const string ErrorLifetimeDefect = "DIE041";
    // Type cannot be inferred.
    public const string ErrorTypeCannotBeInferred = "DIE042";
    // Unhandled generator error.
    public const string ErrorUnhandled = "DIE043";
    // Setup context name is required.
    public const string ErrorSetupContextNameIsRequired = "DIE044";

    // Warnings
    // Binding has been overridden.
    public const string WarningOverriddenBinding = "DIW000";
    // No composition roots defined.
    public const string WarningNoRoots = "DIW001";
    // Implementation does not implement the contract.
    public const string WarningNotImplementedContract = "DIW002";
    // Binding is not used.
    public const string WarningBindingNotUsed = "DIW003";
    // Tag.On injection site is not used.
    public const string WarningInjectionSiteNotUsed = "DIW004";
    // Resolve methods are incompatible with root arguments.
    public const string WarningRootArgInResolveMethod = "DIW005";
    // Resolve methods are incompatible with type arguments.
    public const string WarningTypeArgInResolveMethod = "DIW006";
    // DependsOn uses an instance member.
    public const string WarningInstanceMemberInDependsOnSetup = "DIW007";
    // GenerateInterface attribute is used on a non-public element.
    public const string WarningGenerateInterfaceOnNonPublicMember = "DIW008";
    // GenerateInterface attribute is used on a static element.
    public const string WarningGenerateInterfaceOnStaticMember = "DIW009";
    // Selective interface generation produced an empty interface.
    public const string WarningGenerateInterfaceSelectiveEmpty = "DIW010";

    // Info
    // Generation was interrupted.
    public const string InfoGenerationInterrupted = "DII000";
    // Implementation does not implement the contract.
    public const string InfoNotImplementedContract = "DII001";
}
