// ReSharper disable InconsistentNaming
namespace Pure.DI.Core
{
    internal static class Diagnostics
    {
        internal static class Error
        {
            public const string Unhandled = "DIE0001";
            public const string Unsupported = "DIE0001";
            public const string CircularDependency = "DIE0002";
            public const string CannotFindCtor = "DIE0003";
            public const string MemberIsInaccessible = "DIE0004";
            public const string CannotResolveDependency = "DIE0005";
            public const string CannotResolveLifetime = "DIE0006";
        }

        internal static class Warning
        {
            public const string CannotResolveDependency = "DIW0001";
            public const string CtorIsObsoleted = "DIW0002";
        }

        internal static class Information
        {
            public const string Generated = "DII0001";
            public const string BindingAlreadyExists = "DII0002";
        }
    }
}
