namespace Pure.DI.Tests
{
    using System.Collections.Generic;
    using Core;
    using Shouldly;
    using Xunit;

    public class ConstructorOrderTests
    {
        public static IEnumerable<object?[]> Data =>
            new List<object?[]>
            {
                // Has no any specified order
                new object?[] { new ConstructorOrder(null, 2), new ConstructorOrder(null, 1), false },
                new object?[] { new ConstructorOrder(null, 2), new ConstructorOrder(null, 2), null },
                new object?[] { new ConstructorOrder(null, 1), new ConstructorOrder(null, 2), true },

                // Has no all specified orders
                new object?[] { new ConstructorOrder(0, 0), new ConstructorOrder(1, 0), false },
                new object?[] { new ConstructorOrder(1, 0), new ConstructorOrder(1, 0), null },
                new object?[] { new ConstructorOrder(1, 0), new ConstructorOrder(0, 0), true },

                new object?[] { new ConstructorOrder(0, 1), new ConstructorOrder(1, 2), false },
                new object?[] { new ConstructorOrder(1, 2), new ConstructorOrder(0, 1), true },
                new object?[] { new ConstructorOrder(2, 1), new ConstructorOrder(1, 2), true },
                new object?[] { new ConstructorOrder(1, 0), new ConstructorOrder(2, 1), false },

                new object?[] { new ConstructorOrder(null, 2), new ConstructorOrder(1, 1), true },
                new object?[] { new ConstructorOrder(1, 2), new ConstructorOrder(null, 1), false },
                new object?[] { new ConstructorOrder(null, 1), new ConstructorOrder(1, 2), true },
                new object?[] { new ConstructorOrder(1, 1), new ConstructorOrder(null, 2), false }
            };

        [Theory]
        [MemberData(nameof(Data))]
        internal void A(ConstructorOrder order1, ConstructorOrder order2, bool? makeReorder)
        {
            // Given

            // When
            var result = order1.CompareTo(order2);

            // Then
            if (makeReorder == true)
            {
                result.ShouldBeGreaterThan(0);
            }

            if (makeReorder == false)
            {
                result.ShouldBeLessThan(0);
            }

            if (makeReorder == null)
            {
                result.ShouldBe(0);
            }
        }
    }
}
