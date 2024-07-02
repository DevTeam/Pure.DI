// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable RedundantNameQualifier
// ReSharper disable InvalidXmlDocComment
namespace Pure.DI.Abstractions;

/// <summary>
/// Represents a generic type argument attribute. It allows you to create custom generic type argument marker. 
/// <example>
/// <code>
/// [GenericTypeArgument]
/// internal interface TTMy: IMy { }
/// </code>
/// </example>
/// </summary>
[global::System.AttributeUsage(global::System.AttributeTargets.Class | global::System.AttributeTargets.Interface | global::System.AttributeTargets.Struct)]
public sealed class GenericTypeArgumentAttribute : global::System.Attribute;