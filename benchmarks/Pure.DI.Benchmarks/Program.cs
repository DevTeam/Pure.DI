// ReSharper disable UnusedMember.Local
// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Benchmarks;

using BenchmarkDotNet.Running;

public class Program
{
    public static int Main(string[] args)
    {
        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
        return 0;
    }
}