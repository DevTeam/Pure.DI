namespace Pure.DI.Core;

[Serializable]
internal class HandledException : Exception
{
    public HandledException(string error) : base(error) { }
}