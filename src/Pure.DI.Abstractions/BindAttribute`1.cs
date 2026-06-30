// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedTypeParameter
// ReSharper disable RedundantNameQualifier
// ReSharper disable InvalidXmlDocComment

#pragma warning disable CS9113 // Parameter is unread.
namespace Pure.DI.Abstractions;

using System;

/// <summary>
///     A universal DI attribute that allows to specify a contract, lifetime, and tags for an implementation type.
/// </summary>
/// <param name="lifetime">The binding lifetime. See also <see cref="IBinding.As" />.</param>
/// <param name="tags">The binding tags. See also <see cref="IBinding.Tags" />.</param>
/// <typeparam name="T">The contract type. See also <see cref="IConfiguration.Bind{T}" /> and <see cref="IBinding.Bind{T}" />.</typeparam>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true)]
#if !NET20 && !NET35 && !NETSTANDARD1_0 && !NETSTANDARD1_1 && !NETSTANDARD1_2 && !NETSTANDARD1_3 && !NETSTANDARD1_4 && !NETSTANDARD1_5 && !NETSTANDARD1_6 && !NETCOREAPP1_0 && !NETCOREAPP1_1
[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
#endif
public sealed class BindAttribute<T>(object? lifetime = null, params object?[] tags) : Attribute;
