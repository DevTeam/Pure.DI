namespace Pure.DI.Core;

using System.Globalization;

interface IGlobalProperties
{
    int MaxIterations { get; }

    CultureInfo? Culture { get; }

    bool TryGetProfilePath(out string path);
}