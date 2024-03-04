namespace Pure.DI.Core;

internal interface IGlobalOptions
{
    int MaxIterations { get; }

    bool TryGetProfilePath([NotNullWhen(true)] out string path);
}