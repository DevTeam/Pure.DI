// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable RedundantNameQualifier
// ReSharper disable InvalidXmlDocComment

namespace Pure.DI.Abstractions;

using System;

/// <summary>
///     Marks a type as a custom generic type argument marker for Pure.DI.
/// </summary>
/// <remarks>
///     Generic type argument markers are placeholder types that let you describe open generic bindings. When
///     Pure.DI encounters a marker inside a binding it treats it as "any type" and substitutes the real type
///     argument at the point of injection. This attribute lets you define your own markers in addition to the
///     built-in <c>TT</c>, <c>TT1</c>, <c>TT2</c>, … markers, which is useful when a marker must carry a
///     specific constraint (for example implement a particular interface).
/// </remarks>
/// <example>
///     Declare a constrained marker and use it in a generic binding:
///     <code>
/// [GenericTypeArgument]
/// internal interface TTMy : IMy { }
///
/// DI.Setup("Composition")
///     .Bind&lt;IService&lt;TTMy&gt;&gt;().To&lt;Service&lt;TTMy&gt;&gt;();
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Struct)]
#if !NET20 && !NET35 && !NETSTANDARD1_0 && !NETSTANDARD1_1 && !NETSTANDARD1_2 && !NETSTANDARD1_3 && !NETSTANDARD1_4 && !NETSTANDARD1_5 && !NETSTANDARD1_6 && !NETCOREAPP1_0 && !NETCOREAPP1_1
[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
#endif
public sealed class GenericTypeArgumentAttribute : Attribute;
