namespace Build.Benchmarks;

using Immutype;

[ExcludeFromCodeCoverage]
[Target]
public readonly record struct Statistics(
    int N = 0,
    double Min = .0,
    double LowerFence = .0,
    double Q1 = .0,
    double Median = .0,
    double Mean = .0,
    double Q3 = .0,
    double UpperFence = .0,
    double Max = .0,
    double InterquartileRange = .0,
    double StandardError = .0,
    double Variance = .0,
    double StandardDeviation = .0,
    double Skewness = .0,
    double Kurtosis = .0);