// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable RedundantNameQualifier
// ReSharper disable InvalidXmlDocComment

#pragma warning disable CS9113 // Parameter is unread.
namespace Pure.DI.Abstractions;

/// <summary>
/// A universal DI attribute that allows to specify the tag and ordinal of an injection.
/// </summary>
/// <param name="tag">The injection tag. See also <see cref="IBinding.Tags"/></param>.
/// <param name="ordinal">The injection ordinal.</param>
[global::System.AttributeUsage(global::System.AttributeTargets.Constructor | global::System.AttributeTargets.Method | global::System.AttributeTargets.Parameter | global::System.AttributeTargets.Property | global::System.AttributeTargets.Field)]
#if !NET20 && !NET35 && !NETSTANDARD1_0 && !NETSTANDARD1_1 && !NETSTANDARD1_2 && !NETSTANDARD1_3 && !NETSTANDARD1_4 && !NETSTANDARD1_5 && !NETSTANDARD1_6 && !NETCOREAPP1_0 && !NETCOREAPP1_1
[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
#endif
public sealed class InjectAttribute(object? tag = default, int ordinal = default) : global::System.Attribute;