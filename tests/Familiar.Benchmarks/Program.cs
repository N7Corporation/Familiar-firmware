using BenchmarkDotNet.Running;

namespace Familiar.Benchmarks;

public class Program
{
    public static void Main(string[] args)
    {
        // Run all benchmarks in this assembly
        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
    }
}
