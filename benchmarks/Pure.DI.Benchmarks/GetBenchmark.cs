// ReSharper disable SuggestVarOrType_BuiltInTypes
#pragma warning disable CS8500
namespace Pure.DI.Benchmarks;

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using BenchmarkDotNet.Attributes;

public class GetBenchmark
{
    private const int MaxItems = 10;
    private static readonly Pair<Type, string>[] Pairs;
    private static readonly uint Divisor;
    private static readonly int BucketSize;
    private static readonly Type Key1;
    private static readonly Type Key2;
    private static readonly Type Key3;
    private static readonly Type Key4;
    private static readonly Type Key5;
    private static readonly Type Key6;
    private static readonly Type Key7;
    private static readonly Type Key8;
    private static readonly Type Key9;
    private static readonly Type Key10;

    static GetBenchmark()
    {
        var data = CreatePairs();
        Divisor = Buckets<Type, string>.GetDivisor((uint)data.Length);
        Pairs = Buckets<Type, string>.Create(
            Divisor,
            out BucketSize,
            data);

        Key1 = data[1].Key;
        Key2 = data[2].Key;
        Key3 = data[3].Key;
        Key4 = data[4].Key;
        Key5 = data[5].Key;
        Key6 = data[6].Key;
        Key7 = data[7].Key;
        Key8 = data[8].Key;
        Key9 = data[9].Key;
        Key10 = data[0].Key;
    }

    [Benchmark(OperationsPerInvoke = 10)]
    [SuppressMessage("Performance", "CA1822:Mark members as static")]
    public string GetFor()
    {
        GetFor(Key1);
        GetFor(Key2);
        GetFor(Key3);
        GetFor(Key4);
        GetFor(Key5);
        GetFor(Key6);
        GetFor(Key7);
        GetFor(Key8);
        GetFor(Key9);
        return GetFor(Key10);
    }
    
    [Benchmark(OperationsPerInvoke = 10)]
    [SuppressMessage("Performance", "CA1822:Mark members as static")]
    public string GetForeach()
    {
        GetForeach(Key1);
        GetForeach(Key2);
        GetForeach(Key3);
        GetForeach(Key4);
        GetForeach(Key5);
        GetForeach(Key6);
        GetForeach(Key7);
        GetForeach(Key8);
        GetForeach(Key9);
        return GetFor(Key10);
    }
    
    [Benchmark(OperationsPerInvoke = 10, Baseline = true)]
    [SuppressMessage("Performance", "CA1822:Mark members as static")]
    public string Get()
    {
        Get(Key1);
        Get(Key2);
        Get(Key3);
        Get(Key4);
        Get(Key5);
        Get(Key6);
        Get(Key7);
        Get(Key8);
        Get(Key9);
        return Get(Key10);
    }
    
    [Benchmark(OperationsPerInvoke = 10)]
    [SuppressMessage("Performance", "CA1822:Mark members as static")]
    public string GetWhile()
    {
        GetWhile(Key1);
        GetWhile(Key2);
        GetWhile(Key3);
        GetWhile(Key4);
        GetWhile(Key5);
        GetWhile(Key6);
        GetWhile(Key7);
        GetWhile(Key8);
        GetWhile(Key9);
        return GetWhile(Key10);
    }
    
    [Benchmark(OperationsPerInvoke = 10)]
    [SuppressMessage("Performance", "CA1822:Mark members as static")]
    public string GetUnsafe()
    {
        GetUnsafe(Key1);
        GetUnsafe(Key2);
        GetUnsafe(Key3);
        GetUnsafe(Key4);
        GetUnsafe(Key5);
        GetUnsafe(Key6);
        GetUnsafe(Key7);
        GetUnsafe(Key8);
        GetUnsafe(Key9);
        return GetUnsafe(Key10);
    }
    
    private static string GetFor(Type key)
    {
        int index = (int)(BucketSize * ((uint)RuntimeHelpers.GetHashCode(key) % Divisor));
        for (int i = index; i < index + BucketSize; i++)
        {
            ref var pair = ref Pairs[i];
            if (ReferenceEquals(pair.Key, key))
            {
                return pair.Value;
            }
        }

        throw new InvalidOperationException();
    }
    
    private static string GetForeach(Type key)
    {
        int index = (int)(BucketSize * ((uint)RuntimeHelpers.GetHashCode(key) % Divisor));
        foreach (ref var pair in Pairs.AsSpan(index, BucketSize))
        {
            if (ReferenceEquals(pair.Key, key))
            {
                return pair.Value;
            }
        }

        throw new InvalidOperationException();
    }
    
    private static string Get(Type key)
    {
        int index = (int)(BucketSize * ((uint)RuntimeHelpers.GetHashCode(key) % Divisor));
        ref var pair = ref Pairs[index];
        if (ReferenceEquals(pair.Key, key))
        {
            return pair.Value;
        }

        for (int i = index + 1; i < index + BucketSize; i++)
        {
            pair = ref Pairs[i];
            if (ReferenceEquals(pair.Key, key))
            {
                return pair.Value;
            }
        }

        throw new InvalidOperationException();
    }
    
    private static string GetWhile(Type key)
    {
        int index = (int)(BucketSize * ((uint)RuntimeHelpers.GetHashCode(key) % Divisor));
        var finish = index + BucketSize;
        do
        {
            ref var pair = ref Pairs[index];
            if (ReferenceEquals(pair.Key, key))
            {
                return pair.Value;
            }
        } while(++index < finish);

        throw new InvalidOperationException();
    }

    private static unsafe string GetUnsafe(Type key)
    {
        fixed (Pair<Type, string>* pairsPtr = Pairs)
        {
            var pairPtr = pairsPtr + BucketSize * ((uint)RuntimeHelpers.GetHashCode(key) % Divisor);
            var finishPtr = pairPtr + BucketSize;
            do
            {
                if (ReferenceEquals(pairPtr->Key, key))
                {
                    return pairPtr->Value;
                }
            } while (++pairPtr < finishPtr);
        }

        throw new InvalidOperationException();
    }

    private static Pair<Type, string>[] CreatePairs() =>
        Assembly.GetExecutingAssembly().GetTypes()
            .Take(MaxItems)
            .Select(i => new Pair<Type, string>(i, i.ToString()))
            .ToArray();
}