# Diagnostics Reference

This document is the stable reference for diagnostic IDs, descriptions, and
example scenarios. IDs and anchors are stable; message text is localized.

## Errors

### DIE000
- Description: Unable to resolve a dependency in the object graph.
- Problem: The generator cannot find a binding or auto-binding that satisfies a required injection.
- Fix: Add a binding for the missing abstraction or enable/allow auto-binding for the concrete type.
- See: [injections-of-abstractions](readme/injections-of-abstractions.md).
- Examples: A root depends on a service that has no binding.

### DIE001
- Description: Binding cannot be built due to missing binding metadata.
- Problem: A binding is missing required information (type, source, or factory).
- Fix: Ensure the binding has a valid `.Bind(...).To<...>()` or `.Bind(...).To(ctx => ...)` chain.
- See: [simplified-binding](readme/simplified-binding.md).
- Examples: Binding is incomplete or the setup call is malformed.

### DIE002
- Description: Binding is invalid because the compiler reported errors.
- Problem: The compiler produced errors in code used by the binding.
- Fix: Fix compilation errors in the referenced type or factory code.
- See: [simplified-binding](readme/simplified-binding.md).
- Examples: Binding references a type that failed to compile.

### DIE003
- Description: Composition type name is invalid.
- Problem: The composition name is not a valid C# type name.
- Fix: Use a valid identifier or fully qualified type name (letters/digits/underscore).
- See: [composition-roots](readme/composition-roots.md).
- Examples: `DI.Setup("Bad Name!")`.

### DIE004
- Description: Root name is invalid.
- Problem: The root property/method name is not a valid identifier.
- Fix: Use a valid C# identifier without spaces or punctuation.
- See: [composition-roots](readme/composition-roots.md).
- Examples: `Root<Service>("Bad Name!")`.

### DIE005
- Description: Duplicate root names detected.
- Problem: Multiple roots produce the same generated member name.
- Fix: Rename one of the roots to a unique name.
- See: [composition-roots](readme/composition-roots.md).
- Examples: Two roots with the same name.

### DIE006
- Description: Argument name is invalid.
- Problem: Argument name is not a valid C# identifier.
- Fix: Use a valid identifier, for example `argName` or `timeoutSeconds`.
- See: [composition-arguments](readme/composition-arguments.md).
- Examples: `Arg<int>("bad name")`.

### DIE007
- Description: Composition argument type is based on a generic marker.
- Problem: Composition arguments cannot use `TT` or marker-based types.
- Fix: Replace marker types with concrete types or proper generic parameters.
- See: [composition-arguments](readme/composition-arguments.md).
- Examples: `Arg<TT>("arg")`.

### DIE008
- Description: Accumulator type is based on a generic marker.
- Problem: Accumulators cannot be defined with marker-based types.
- Fix: Use a concrete accumulator type or a valid generic type.
- See: [accumulators](readme/accumulators.md).
- Examples: `Accumulator<TT>()`.

### DIE009
- Description: Accumulator cannot accumulate a generic marker.
- Problem: The accumulated element type is marker-based.
- Fix: Use concrete types or valid generic arguments for accumulated values.
- See: [accumulators](readme/accumulators.md).
- Examples: Accumulator value uses `TT`.

### DIE010
- Description: Special type cannot be a generic marker.
- Problem: `SpecialType<T>` does not accept marker-based types.
- Fix: Use concrete or supported special types.
- See: [simplified-binding](readme/simplified-binding.md).
- Examples: `SpecialType<IComparer<TT>>()`.

### DIE011
- Description: Implementation does not implement the contract.
- Problem: The bound implementation does not satisfy the abstraction.
- Fix: Bind the correct implementation or change the contract to match.
- See: [injections-of-abstractions](readme/injections-of-abstractions.md).
- Examples: `Bind<IService>().To<Service>()` where `Service` does not implement `IService`.

### DIE012
- Description: Attribute argument position is invalid.
- Problem: The attribute argument index does not exist.
- Fix: Use a valid argument index or adjust the attribute definition.
- See: [custom-attributes](readme/custom-attributes.md).
- Examples: Attribute argument index is out of range.

### DIE013
- Description: Attribute member cannot be processed.
- Problem: The attribute usage is ambiguous or conflicting.
- Fix: Use a single, unambiguous attribute or remove duplicates.
- See: [custom-attributes](readme/custom-attributes.md).
- Examples: Multiple attributes cause ambiguity.

### DIE014
- Description: Root type is invalid.
- Problem: Root declaration does not provide a valid type argument.
- Fix: Use `Root<T>` with a valid type.
- See: [roots](readme/roots.md).
- Examples: `Root()` without a type argument.

### DIE015
- Description: Roots type is invalid.
- Problem: The roots API requires a non-`object` type.
- Fix: Use a specific service/abstraction type.
- See: [roots](readme/roots.md).
- Examples: `Roots<object>()`.

### DIE016
- Description: No type matched the wildcard filter.
- Problem: The wildcard filter excluded all candidate types.
- Fix: Adjust the filter or remove it to include matching types.
- See: [roots-with-filter](readme/roots-with-filter.md).
- Examples: `Roots<IService>(filter: "NoMatch*")`.

### DIE017
- Description: Builders type is invalid.
- Problem: The builders API requires a non-`object` type.
- Fix: Use a specific type for builders.
- See: [builders](readme/builders.md).
- Examples: `Builders<object>()`.

### DIE018
- Description: Builder type is invalid.
- Problem: Builder declaration does not provide a valid type argument.
- Fix: Use `Builder<T>` with a valid type.
- See: [builder](readme/builder.md).
- Examples: `Builder()` without a type argument.

### DIE019
- Description: Too many type parameters provided.
- Problem: The API call uses more generic arguments than supported.
- Fix: Remove extra type arguments or use the correct overload.
- See: [generics](readme/generics.md).
- Examples: Invalid generic API usage.

### DIE020
- Description: Async factories are not supported.
- Problem: `async` lambdas are not supported in factory bindings.
- Fix: Use a synchronous factory or wrap async logic outside the composition.
- See: [factory](readme/factory.md).
- Examples: `.To(async ctx => ...)`.

### DIE021
- Description: Unsupported syntax in DI API.
- Problem: The generator encountered an expression it cannot process.
- Fix: Use supported API patterns (constants, simple expressions, or factories).
- See: [factory](readme/factory.md).
- Examples: Unsupported expressions passed into `Bind/To/Root`.

### DIE022
- Description: Value must be of a specific type.
- Problem: A constant of the expected type is required.
- Fix: Provide a constant literal or use a supported API call.
- See: [tags](readme/tags.md).
- Examples: Tag or hint expects a constant value.

### DIE023
- Description: Identifier is invalid.
- Problem: A name used in setup is not a valid identifier.
- Fix: Use a valid identifier (letters/digits/underscore; no spaces).
- See: [composition-roots](readme/composition-roots.md).
- Examples: Root or builder name is not a valid C# identifier.

### DIE024
- Description: Context is used directly where it is not allowed.
- Problem: `ctx` is used as a value rather than calling its methods.
- Fix: Replace direct usage with `ctx.Inject(...)` or `ctx.Override(...)`.
- See: [factory](readme/factory.md).
- Examples: `ctx` used as a value instead of `ctx.Inject(...)`.

### DIE025
- Description: Initializers count does not match.
- Problem: Factory initializers do not align with generated markers.
- Fix: Ensure the factory uses only supported initializer patterns.
- See: [factory](readme/factory.md).
- Examples: Factory code does not match initializer markers.

### DIE026
- Description: Lifetime does not support cyclic dependencies.
- Problem: The dependency graph contains cycles incompatible with the lifetime.
- Fix: Break the cycle or change lifetimes to compatible ones.
- See: [scope](readme/scope.md).
- Examples: Singleton depending on Scoped with cycles.

### DIE027
- Description: Composition is too large to build.
- Problem: Graph exploration exceeded the maximum iteration limit.
- Fix: Reduce graph size or adjust limits if supported.
- See: [composition-roots](readme/composition-roots.md).
- Examples: Graph iteration limit exceeded.

### DIE028
- Description: Cannot construct an abstract type.
- Problem: The binding points to an abstract class or interface.
- Fix: Bind to a concrete implementation type.
- See: [auto-bindings](readme/auto-bindings.md).
- Examples: Binding to an abstract class.

### DIE029
- Description: No accessible constructor found.
- Problem: No public/internal constructors are available.
- Fix: Add an accessible constructor or use a factory binding.
- See: [auto-bindings](readme/auto-bindings.md).
- Examples: Only private constructors are available.

### DIE030
- Description: Dependency graph cannot be built.
- Problem: The graph cannot converge to a resolved state.
- Fix: Check for missing bindings or conflicting lifetimes.
- See: [resolve-methods](readme/resolve-methods.md).
- Examples: Variational graph resolution failed.

### DIE031
- Description: Maximum number of iterations reached.
- Problem: Graph building exceeded configured iteration limit.
- Fix: Simplify the graph or increase the limit.
- See: [composition-roots](readme/composition-roots.md).
- Examples: Graph build exceeded max iterations.

### DIE032
- Description: No accessible constructor found for Tag.On.
- Problem: No constructor matches the Tag.On criteria.
- Fix: Update the type or the Tag.On arguments to match a constructor.
- See: [tag-on-a-constructor-argument](readme/tag-on-a-constructor-argument.md).
- Examples: `Tag.OnConstructorArg<T>(...)` with no matching constructor.

### DIE033
- Description: No accessible method found for Tag.On.
- Problem: No method matches the Tag.On criteria.
- Fix: Update the type or the Tag.On arguments to match a method.
- See: [tag-on-a-method-argument](readme/tag-on-a-method-argument.md).
- Examples: `Tag.OnMethodArg<T>(...)` with no matching method.

### DIE034
- Description: No accessible field or property found for Tag.On.
- Problem: No field/property matches the Tag.On criteria.
- Fix: Update the type or the Tag.On arguments to match a member.
- See: [tag-on-a-member](readme/tag-on-a-member.md).
- Examples: `Tag.OnMember<T>(...)` with no matching member.

### DIE035
- Description: Expression must be a valid API call.
- Problem: A non-constant or unsupported expression was provided.
- Fix: Use a constant, a supported API call, or rewrite the expression.
- See: [tags](readme/tags.md).
- Examples: Passing a non-constant expression to Tag or Hint.

### DIE036
- Description: Regular expression is invalid.
- Problem: The regex pattern cannot be parsed.
- Fix: Fix the regex syntax or escape special characters.
- See: [oncannotresolve-regular-expression-hint](readme/oncannotresolve-regular-expression-hint.md).
- Examples: Invalid regex in a hint filter.

### DIE037
- Description: Wildcard is invalid.
- Problem: The wildcard pattern is malformed.
- Fix: Use supported wildcard syntax.
- See: [oncannotresolve-wildcard-hint](readme/oncannotresolve-wildcard-hint.md).
- Examples: Invalid wildcard pattern in a hint filter.

### DIE038
- Description: DI setup could not be found.
- Problem: DependsOn refers to a setup that does not exist.
- Fix: Add the missing setup or correct the name.
- See: [dependent-compositions](readme/dependent-compositions.md).
- Examples: `DependsOn("MissingSetup")`.

### DIE039
- Description: Cyclic dependency detected.
- Problem: A dependency cycle exists in the graph.
- Fix: Break the cycle or introduce a factory/Func to defer construction.
- See: [injection-on-demand](readme/injection-on-demand.md).
- Examples: `A -> B -> A`.

### DIE040
- Description: Language version is not supported.
- Problem: The project uses a C# version below the required minimum.
- Fix: Increase the language version in project settings.
- See: [composition-roots](readme/composition-roots.md).
- Examples: Using C# language version below the minimum.

### DIE041
- Description: Lifetime validation error.
- Problem: A root/lifetime combination is not allowed.
- Fix: Change lifetimes or root kind to a compatible configuration.
- See: [static-root](readme/static-root.md).
- Examples: Static root depends on Scoped/Singleton in unsupported way.

### DIE042
- Description: Type cannot be inferred.
- Problem: The generator cannot determine the type from the expression.
- Fix: Specify the type explicitly (generic argument or cast).
- See: [overrides](readme/overrides.md).
- Examples: `Override(...)` without an inferable type.

### DIE043
- Description: Unhandled generator error.
- Problem: An unexpected exception occurred in the generator.
- Fix: Reduce the scenario and report a bug with repro steps.
- See: [check-for-a-root](readme/check-for-a-root.md).

### DIE044
- Description: DependsOn requires a setup context name when using a setup context kind (except Members).
- Problem: `DependsOn` was called with a setup context kind but without a context name.
- Fix: Provide a non-empty `name` value or use `DependsOn(params string[] setupNames)` when a context is not needed. `SetupContextKind.Members` allows omitting `name`.
- Examples: `DependsOn(setupName, contextKind)` without providing `name` parameter.

## Warnings

### DIW000
- Description: Binding has been overridden.
- Problem: A later binding replaces an earlier one for the same contract/tag.
- Fix: Remove the earlier binding or add tags to distinguish them.
- See: [overriding-the-bcl-binding](readme/overriding-the-bcl-binding.md).
- Examples: Two bindings for the same contract/tag, the latter wins.

### DIW001
- Description: No composition roots defined.
- Problem: The composition exposes no roots.
- Fix: Add at least one `.Root(...)` declaration.
- See: [composition-roots](readme/composition-roots.md).
- Examples: `DI.Setup("Composition")` without any `.Root(...)`.

### DIW002
- Description: Implementation does not implement the contract.
- Problem: The implementation does not satisfy the requested contract.
- Fix: Use a correct implementation or adjust the contract.
- See: [injections-of-abstractions](readme/injections-of-abstractions.md).
- Examples: Severity is Warning for not implemented contract.

### DIW003
- Description: Binding is not used.
- Problem: The binding is never requested by any root.
- Fix: Remove unused bindings or reference them in a root.
- See: [composition-roots](readme/composition-roots.md).
- Examples: Binding has no consumers in the graph.

### DIW004
- Description: Tag.On injection site is not used.
- Problem: The tag-on site is never matched by any injection.
- Fix: Update the Tag.On expression or add matching injections.
- See: [tag-on-injection-site](readme/tag-on-injection-site.md).
- Examples: `Tag.On(...)` not referenced by any injection.

### DIW005
- Description: Resolve methods are incompatible with root arguments.
- Problem: `Resolve` cannot provide values for root arguments.
- Fix: Disable Resolve methods or remove RootArg usage.
- See: [resolve-methods](readme/resolve-methods.md).
- Examples: Root uses `RootArg` but `Resolve` is enabled.

### DIW006
- Description: Resolve methods are incompatible with type arguments.
- Problem: `Resolve` cannot supply generic type arguments.
- Fix: Use explicit roots or avoid Resolve for generic roots.
- See: [resolve-methods](readme/resolve-methods.md).
- Examples: Generic roots with `Resolve` methods.

### DIW007
- Description: DependsOn uses an instance member.
- Problem: A binding in a dependent setup references an instance member that will not exist in the dependent composition.
- Fix: Use `.DependsOn(setupName, contextArgName)` to pass an explicit setup context, or move the value to `Arg/RootArg` or a separate context object.
- See: [dependent-compositions](readme/dependent-compositions.md).
- Examples: Binding in a dependent setup uses instance field/property.

### DIW008
- Description: GenerateInterface is applied to a non-public element.
- Problem: `[GenerateInterface]` was placed on a non-public method/property/event.
- Fix: Make the element `public` or remove the attribute from that element.
- See: [generate-interface](readme/generate-interface.md).
- Examples: `[GenerateInterface]` on `private` or `internal` property.

### DIW009
- Description: GenerateInterface is applied to a static element.
- Problem: `[GenerateInterface]` was placed on a `static` method/property/event.
- Fix: Move the contract element to an instance member or remove the attribute.
- See: [generate-interface](readme/generate-interface.md).
- Examples: `[GenerateInterface]` on `public static` method.

### DIW010
- Description: Selective interface generation produced an empty interface.
- Problem: Selective mode was enabled for an interface, but no eligible elements remained after filtering.
- Fix: Mark at least one `public` instance element for that interface, or remove selective member annotations.
- See: [generate-interface](readme/generate-interface.md).
- Examples: All selected elements are `IgnoreInterface`, non-public, or static.

## Info

### DII000
- Description: Generation was interrupted.
- Problem: Generation aborted early due to a handled exception.
- Fix: Inspect preceding errors for the root cause.
- See: [check-for-a-root](readme/check-for-a-root.md).
- Examples: Generation aborted due to a handled exception.

### DII001
- Description: Implementation does not implement the contract.
- Problem: The implementation does not satisfy the requested contract.
- Fix: Use a correct implementation or adjust the contract.
- See: [injections-of-abstractions](readme/injections-of-abstractions.md).
- Examples: Severity is Info for not implemented contract.
