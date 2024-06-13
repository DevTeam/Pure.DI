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
public sealed class InjectAttribute(object? tag = default, int ordinal = default) : global::System.Attribute;