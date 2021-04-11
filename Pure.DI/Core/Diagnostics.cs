namespace Pure.DI.Core
{
    internal static class Diagnostics
    {
        // Errors
        public const string Unhandled = "DIE0001";
        public const string Unsupported = "DIE0001";
        public const string CircularDependency = "DIE0002";
        public const string CannotFindCtor = "DIE0003";
        public const string MemberIsInaccessible = "DIE0004";
        public const string CannotResolveDependencyError = "DIW0005";

        // Warnings
        public const string CannotResolveDependencyWarning = "DIW0001";
        public const string CtorIsObsoleted = "DIW0002";
        public const string BindingIsAlreadyExist = "DIW0003";

        // Information
        public const string Generated = "DII0001";
        public const string CannotUseCurrentType = "DII0002";
    }
}
