namespace Pure.DI.Tests
{
    using System.Collections.Generic;
    using System.Linq;
    using Components;
    using Shouldly;
    using Xunit;

    public class TableTests
    {
        [Fact]
        public void ShouldProvideTryGet()
        {
            // Given
            var count = 10;
            var pairs =
                (from index in Enumerable.Range(-count, count * 2)
                    select new KeyValuePair<long, long>(index, index)).ToArray();

            // When
            var table = new Table<long, long>(pairs);

            // Then
            for (var index = -count; index < count; index++)
            {
                table.TryGet(index, out var val).ShouldBeTrue();
                val.ShouldBe(index);
            }

            table.TryGet(count, out _).ShouldBeFalse();
        }

        [Fact]
        public void ShouldProvideTryGetWhenEmpty()
        {
            // Given

            // When
            var table = new Table<long, long>(new List<KeyValuePair<long, long>>());

            // Then
            table.TryGet(10, out _).ShouldBeFalse();
        }
    }
}
