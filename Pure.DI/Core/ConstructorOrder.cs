namespace Pure.DI.Core
{
    using System;
    using System.Collections.Generic;

    internal class ConstructorOrder: IComparable<ConstructorOrder>
    {
        private readonly IComparable? _specifiedOrder;
        private readonly int _weight;

        public ConstructorOrder(IComparable? specifiedOrder, int weight)
        {
            _specifiedOrder = specifiedOrder;
            _weight = weight;
        }

        public int CompareTo(ConstructorOrder other)
        {
            if (_specifiedOrder == null && other._specifiedOrder == null)
            {
                return Comparer<int>.Default.Compare(other._weight, _weight);
            }

            if (_specifiedOrder != null && other._specifiedOrder != null)
            {
                return _specifiedOrder.CompareTo(other._specifiedOrder);
            }

            return _specifiedOrder == null ? 1 : -1;
        }
    }
}