﻿// ReSharper disable ClassNeverInstantiated.Global

namespace Pure.DI.Core;

sealed class IdGenerator : IIdGenerator
{
    private int _id = -1;

    public int Generate() => Interlocked.Increment(ref _id);
}