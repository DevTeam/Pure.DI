namespace Pure.DI.Core
{
    using System;

    [Serializable]
    internal class HandledException: Exception
    {
        public HandledException(string error) : base(error) { }
    }
}
