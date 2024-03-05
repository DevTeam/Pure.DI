namespace Pure.DI.Core;

internal interface IGlobalOptions
{
    int MaxIterations { get; }

    bool TryGetProfilePath(out string path);
}