namespace Pure.DI.Core;

[Serializable]
internal sealed class HandledException : Exception
{
    public HandledException(string error) : base(error) { }
}