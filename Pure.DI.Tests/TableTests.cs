namespace Pure.DI.Tests
{
    using System;
    using System.Linq;
    using Shouldly;
    using Xunit;

    public class TableTests
    {
        [Fact]
        public void ShouldProvideTryGet()
        {
            // Given
            const int count = 10;
            var pairs = (
                from index in Enumerable.Range(-count, count * 2)
                select new Pair<long, long>(index, index)).ToArray();

            // When
            var table = new Table<long, long>(pairs);

            // Then
            for (var index = -count; index < count; index++)
            {
                table.Get(index).ShouldBe(index);
            }

            table.Get(count).ShouldBe(0);
        }

        [Fact]
        public void ShouldProvideTryGetWhenEmpty()
        {
            // Given

            // When
            var table = new Table<string, string>(Array.Empty<Pair<string, string>>());

            // Then
            table.Get("aa").ShouldBeNull();
        }
    }
}
