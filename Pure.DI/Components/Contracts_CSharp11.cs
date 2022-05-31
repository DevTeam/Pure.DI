// ReSharper disable UnusedParameter.Global
// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable NotAccessedField.Global
// ReSharper disable StructCanBeMadeReadOnly
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ArrangeNamespaceBody
#pragma warning disable 0436
#pragma warning disable 8625
namespace NS35EBD81B
{
    using System;

    /// <summary>
    /// Represents a dependency type attribute overriding an injection type. 
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.Field)]
    internal class TypeAttribute<T> : Attribute
    { }
}
#pragma warning restore 0436
#pragma warning restore 8625