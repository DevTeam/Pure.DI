// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedTypeParameter
// ReSharper disable RedundantNameQualifier
// ReSharper disable InvalidXmlDocComment

#pragma warning disable CS9113 // Parameter is unread.
namespace Pure.DI.Abstractions;

using System;

/// <summary>
///     A universal DI attribute that allows to specify the type, tag, and ordinal of an injection.
/// </summary>
/// <param name="tag">The injection tag. See also <see cref="IBinding.Tags" /></param>
/// .
/// <param name="ordinal">The injection ordinal.</param>
/// <typeparam name="T">The injection type. See also <see cref="IConfiguration.Bind{T}" /> and <see cref="IBinding.Bind{T}" /></typeparam>
[AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Method | AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.Field)]
#if !NET20 && !NET35 && !NETSTANDARD1_0 && !NETSTANDARD1_1 && !NETSTANDARD1_2 && !NETSTANDARD1_3 && !NETSTANDARD1_4 && !NETSTANDARD1_5 && !NETSTANDARD1_6 && !NETCOREAPP1_0 && !NETCOREAPP1_1
[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
#endif
public sealed class InjectAttribute<T>(object? tag = null, int ordinal = 0) : Attribute;