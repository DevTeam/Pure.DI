// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable RedundantNameQualifier
// ReSharper disable InvalidXmlDocComment

#pragma warning disable CS9113 // Parameter is unread.
namespace Pure.DI.Abstractions;

using System;

/// <summary>
///     A universal DI attribute that customizes an injection site by specifying its tag and ordinal.
/// </summary>
/// <remarks>
///     Apply this attribute to a constructor, method, parameter, property, or field to control how Pure.DI
///     satisfies that dependency. The <paramref name="tag" /> selects a specific tagged binding, and the
///     <paramref name="ordinal" /> determines the order in which members and initialization methods are
///     injected (lower ordinals are injected first). Use <see cref="InjectAttribute{T}" /> when the injected
///     type differs from the declared member type.
/// </remarks>
/// <param name="tag">The injection tag used to select a specific tagged binding, or <see langword="null" /> for the default binding. See also <see cref="IBinding.Tags" />.</param>
/// <param name="ordinal">The injection ordinal that defines the order of injection for members and initialization methods; lower values are injected first.</param>
/// <example>
///     Inject a tagged dependency into a constructor parameter:
///     <code>
/// class Service(
///     [Inject("primary")] IDependency dependency)
/// {
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Method | AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.Field)]
#if !NET20 && !NET35 && !NETSTANDARD1_0 && !NETSTANDARD1_1 && !NETSTANDARD1_2 && !NETSTANDARD1_3 && !NETSTANDARD1_4 && !NETSTANDARD1_5 && !NETSTANDARD1_6 && !NETCOREAPP1_0 && !NETCOREAPP1_1
[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
#endif
public sealed class InjectAttribute(object? tag = null, int ordinal = 0) : Attribute;