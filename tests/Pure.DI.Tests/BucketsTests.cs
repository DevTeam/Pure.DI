// ReSharper disable SuggestBaseTypeForParameter
// ReSharper disable SuggestVarOrType_BuiltInTypes
// ReSharper disable HeapView.PossibleBoxingAllocation
namespace Pure.DI.Tests;

using System.Runtime.CompilerServices;
using Pure.DI;

public class BucketsTests
{
    [Theory]
    [InlineData(1u)]
    [InlineData(2u)]
    [InlineData(11u)]
    [InlineData(1000u)]
    internal void ShouldGetValues(uint count)
    {
        // Given
        var data = CreatePairs(count);
        var divisor = Buckets<string, int>.GetDivisor(count);

        // When
        var pairs = Buckets<string, int>.Create(
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

    private static Pair<string, int>[] CreatePairs(uint count) => 
        Enumerable.Range(0, (int)count)
            .Select(i => new Pair<string, int>((100 + i).ToString(), i))
            .ToArray();

    [Fact]
    public void ShouldSupportTypeKey()
    {
        // Given
        var divisor = Buckets<Type, int>.GetDivisor(5);
        var pairs = Buckets<Type, int>.Create(
            divisor,
            out var bucketSize,
            [
                new Pair<Type, int>(typeof(string), 1),
                new Pair<Type, int>(typeof(int), 2),
                new Pair<Type, int>(typeof(double), 3),
                new Pair<Type, int>(typeof(float), 4),
                new Pair<Type, int>(typeof(char), 5)
            ]
        );

        // When
        var value = Get(pairs, divisor, bucketSize, typeof(float));

        // Then
        value.ShouldBe(4);
    }

    private static TValue Get<TKey, TValue>(
        Pair<TKey, TValue>[] pairs,
        uint divisor,
        int bucketSize,
        TKey key)
    {
        int index = (int)(bucketSize * ((uint)RuntimeHelpers.GetHashCode(key) % divisor));
        ref var pair = ref pairs[index];
        if (ReferenceEquals(pair.Key, key))
        {
            return pair.Value;
        }
        
        int maxIndex = index + bucketSize;
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
}