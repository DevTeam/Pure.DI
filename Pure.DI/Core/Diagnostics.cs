// ReSharper disable InconsistentNaming
namespace Pure.DI.Core;

internal static class Diagnostics
{
    internal static class Error
    {
        public const string CannotResolve = "DI001";
        public const string Unhandled = "DI002";
        public const string Unsupported = "DI003";
        public const string CircularDependency = "DI004";
        public const string MemberIsInaccessible = "DI005";
        public const string InvalidSetup = "DI006";
    }

    internal static class Warning
    {
        public const string CtorIsObsoleted = "DIW001";
    }

    internal static class Information
    {
        public const string Generated = "DII001";
        public const string BindingAlreadyExists = "DII002";
    }
}