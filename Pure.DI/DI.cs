namespace Pure.DI
{
    using Core;

    public static class DI
    {
        public static IConfiguration Setup(string targetTypeName = "Resolver") => Configuration.Shared;
    }
}