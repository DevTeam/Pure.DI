namespace Pure.DI.Core
{
    internal interface INameService
    {
        string FindName(MemberKey memberKey);

        void ReserveName(string name);
    }
}