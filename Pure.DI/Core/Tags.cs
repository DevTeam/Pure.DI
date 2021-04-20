namespace Pure.DI.Core
{
    internal enum Tags
    {
        Default,
        AutowiringBuilder,
        FactoryBuilder,
        ArrayBuilder,
        EnumerableBuilder,
        AsIsResult,
        GenericResult,
        StaticResolve,
        StaticWithTag,
        GenericStaticResolve,
        GenericStaticWithTag,
        SimpleBuildStrategy,
        GenericBuildStrategy,
        TypeStatementsStrategy,
        TypeAndTagStatementsStrategy,
        TransientLifetime,
        SingletonLifetime,
        ThreadSingletonLifetime,
        ResolveSingletonLifetime,
        CustomLifetime,
        Resolvers,
        MicrosoftDependencyInjection
    }
}
