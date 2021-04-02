// ReSharper disable UnusedParameter.Global
namespace Pure.DI
{
    using Core;

    public static class DI
    {
        public static IConfiguration Setup(string targetTypeName = "") => Configuration.Shared;
    }
}