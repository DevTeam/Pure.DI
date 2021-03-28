namespace Pure.DI
{
    using Core;

    public static class DI
    {
        public static IConfiguration Setup(string targetTypeName = "CompositionRoot") => Configuration.Shared;
    }
}