// ReSharper disable SuggestBaseTypeForParameter
// ReSharper disable SuggestVarOrType_BuiltInTypes
// ReSharper disable HeapView.PossibleBoxingAllocation

namespace Pure.DI.Tests;

using System.Runtime.CompilerServices;

public class BucketsTests
{
    [Theory]
    [InlineData(1u)]
    [InlineData(2u)]
    [InlineData(11u)]
    [InlineData(100u)]
    internal void ShouldGetValues(uint count)
    {
        // Given
        var data = CreatePairs(count);
        var divisor = Buckets<int>.GetDivisor(count);

        // When
        var pairs = Buckets<int>.Create(
            divisor,
            out var bucketSize,
            data);

        // Then
        foreach (var pair in data)
        {
            Get(pairs, divisor, bucketSize, pair.Key).ShouldBe(pair.Value);
        }

        for (var i = count; i < count + 2; i++)
        {
            Should.Throw<InvalidOperationException>(() => Get(pairs, divisor, bucketSize, (100 + i).ToString()));
        }
    }

    private static Pair<int>[] CreatePairs(uint count) => AppDomain.CurrentDomain.GetAssemblies()
        .SelectMany(i => i.ExportedTypes)
        .Distinct()
        .Take((int)count)
        .Select((i, index) => new Pair<int>(i, index))
        .ToArray();

    private static TValue Get<TKey, TValue>(
        Pair<TValue>[] pairs,
        uint divisor,
        uint bucketSize,
        TKey key)
    {
        int index = (int)(bucketSize * ((uint)RuntimeHelpers.GetHashCode(key) % divisor));
        ref var pair = ref pairs[index];
        if (ReferenceEquals(pair.Key, key))
        {
            return pair.Value;
        }

        var maxIndex = index + bucketSize;
        for (int i = index + 1; i < maxIndex; i++)
        {
            pair = ref pairs[i];
            if (ReferenceEquals(pair.Key, key))
            {
                return pair.Value;
            }
        }

        throw new InvalidOperationException();
    }

    [Fact]
    public void ShouldSupportTypeKey()
    {
        // Given
        var divisor = Buckets<int>.GetDivisor(5);
        var pairs = Buckets<int>.Create(
            divisor,
            out var bucketSize,
            [
                new Pair<int>(typeof(string), 1),
                new Pair<int>(typeof(int), 2),
                new Pair<int>(typeof(double), 3),
                new Pair<int>(typeof(float), 4),
                new Pair<int>(typeof(char), 5)
            ]
        );

        // When
        var value = Get(pairs, divisor, bucketSize, typeof(float));

        // Then
        value.ShouldBe(4);
    }
}