// ReSharper disable SuggestVarOrType_BuiltInTypes
// ReSharper disable MemberCanBeMadeStatic.Local
// ReSharper disable RedundantNameQualifier
#pragma warning disable CS8500
namespace Pure.DI.Benchmarks.Tests;

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using BenchmarkDotNet.Attributes;

[SuppressMessage("Performance", "CA1822:Mark members as static")]
[SuppressMessage("Performance", "CA1859:Use concrete types when possible for improved performance")]
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
    private static readonly Dictionary<Type, string> Dictionary;

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

        Dictionary = data.ToDictionary(i => i.Key, i => i.Value);
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = 10)]
    public void Dict()
    {
        Dictionary.TryGetValue(Key1, out _);
        Dictionary.TryGetValue(Key2, out _);
        Dictionary.TryGetValue(Key3, out _);
        Dictionary.TryGetValue(Key4, out _);
        Dictionary.TryGetValue(Key5, out _);
        Dictionary.TryGetValue(Key6, out _);
        Dictionary.TryGetValue(Key7, out _);
        Dictionary.TryGetValue(Key8, out _);
        Dictionary.TryGetValue(Key9, out _);
        Dictionary.TryGetValue(Key10, out _);
    }
    
    [Benchmark(OperationsPerInvoke = 10)]
    public string Resolve()
    {
        Resolve(Key1);
        Resolve(Key2);
        Resolve(Key3);
        Resolve(Key4);
        Resolve(Key5);
        Resolve(Key6);
        Resolve(Key7);
        Resolve(Key8);
        Resolve(Key9);
        Resolve(Key10);
    }

    private string Resolve(global::System.Type type)
    {
        var index = (int)(BucketSize * ((uint)global::System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(type) % Divisor));
        var finish = index + BucketSize;
        do {
            ref var pair = ref Pairs[index];
            if (pair.Key == type)
            {
                return pair.Value;
            }
        } while (++index < finish);
      
        throw new global::System.InvalidOperationException($"Cannot resolve composition root of type {type}.");
    }

    private static Pair<Type, string>[] CreatePairs() =>
        Assembly.GetExecutingAssembly().GetTypes()
            .Take(MaxItems)
            .Select(i => new Pair<Type, string>(i, i.ToString()))
            .ToArray();
}