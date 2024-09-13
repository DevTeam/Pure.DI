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
#if !NET20 && !NET35 && !NETSTANDARD1_0 && !NETSTANDARD1_1 && !NETSTANDARD1_2 && !NETSTANDARD1_3 && !NETSTANDARD1_4 && !NETSTANDARD1_5 && !NETSTANDARD1_6 && !NETCOREAPP1_0 && !NETCOREAPP1_1
[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
#endif
public sealed class GenericTypeArgumentAttribute : global::System.Attribute;