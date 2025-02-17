// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable RedundantNameQualifier
// ReSharper disable InvalidXmlDocComment

namespace Pure.DI.Abstractions;

using System;

/// <summary>
///     Represents a generic type argument attribute. It allows you to create custom generic type argument marker.
///     <example>
///         <code>
/// [GenericTypeArgument]
/// internal interface TTMy: IMy { }
/// </code>
///     </example>
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Struct)]
#if !NET20 && !NET35 && !NETSTANDARD1_0 && !NETSTANDARD1_1 && !NETSTANDARD1_2 && !NETSTANDARD1_3 && !NETSTANDARD1_4 && !NETSTANDARD1_5 && !NETSTANDARD1_6 && !NETCOREAPP1_0 && !NETCOREAPP1_1
[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
#endif
public sealed class GenericTypeArgumentAttribute : Attribute;