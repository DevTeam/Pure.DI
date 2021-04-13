// ReSharper disable NotAccessedField.Global
// ReSharper disable UnusedType.Global
namespace Pure.DI
{
    using System;

    [AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Method | AttributeTargets.Field | AttributeTargets.Property)]
    public class OrderAttribute : Attribute
    {
        // ReSharper disable once MemberCanBePrivate.Global
        public readonly int Order;

        public OrderAttribute(int order) => Order = order;
    }
}
