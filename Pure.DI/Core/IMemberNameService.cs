namespace Pure.DI.Core
{
    internal interface IMemberNameService
    {
        string GetName(MemberNameKind kind);
    }
}