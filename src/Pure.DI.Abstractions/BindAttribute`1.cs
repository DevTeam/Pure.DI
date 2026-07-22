// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedTypeParameter
// ReSharper disable RedundantNameQualifier
// ReSharper disable InvalidXmlDocComment

#pragma warning disable CS9113 // Parameter is unread.
namespace Pure.DI.Abstractions;

using System;

/// <summary>
///     A universal DI attribute that declaratively binds an implementation type to a contract, together with
///     an optional lifetime and tags, directly on the implementation itself.
/// </summary>
/// <remarks>
///     Placing this attribute on a class or a struct is equivalent to writing an explicit binding such as
///     <c>Bind&lt;T&gt;().As(lifetime).Tags(tags).To&lt;TImplementation&gt;()</c> in the composition setup.
///     The attribute can be applied multiple times to expose the same implementation under several contracts.
/// </remarks>
/// <param name="lifetime">
///     The binding lifetime (for example a <c>Pure.DI.Lifetime</c> value such as <c>Singleton</c> or
///     <c>Scoped</c>). When <see langword="null" />, the default <c>Transient</c> lifetime is used.
///     See also <see cref="IBinding.As" />.
/// </param>
/// <param name="tags">The binding tags used to distinguish several implementations of the same contract. See also <see cref="IBinding.Tags" />.</param>
/// <typeparam name="T">The contract type the implementation is bound to. See also <see cref="IConfiguration.Bind{T}" /> and <see cref="IBinding.Bind{T}" />.</typeparam>
/// <example>
///     Bind a service to its interface as a singleton:
///     <code>
/// [Bind&lt;IDependency&gt;(lifetime: Lifetime.Singleton)]
/// class Dependency : IDependency;
/// </code>
///     Expose the same implementation under several contracts with a tag:
///     <code>
/// [Bind&lt;IService&gt;(tags: "primary")]
/// [Bind&lt;IDisposable&gt;]
/// class Service : IService, IDisposable;
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true)]
#if !NET20 && !NET35 && !NETSTANDARD1_0 && !NETSTANDARD1_1 && !NETSTANDARD1_2 && !NETSTANDARD1_3 && !NETSTANDARD1_4 && !NETSTANDARD1_5 && !NETSTANDARD1_6 && !NETCOREAPP1_0 && !NETCOREAPP1_1
[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
#endif
public sealed class BindAttribute<T>(object? lifetime = null, params object?[] tags) : Attribute;
