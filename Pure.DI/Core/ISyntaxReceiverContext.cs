namespace Pure.DI.Core
{
    internal interface ISyntaxReceiverContext
    {
        bool HasChanges { get; }

        void Reset();
    }
}