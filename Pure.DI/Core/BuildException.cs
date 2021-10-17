namespace Pure.DI.Core
{
    using System;
    using Microsoft.CodeAnalysis;

    public class BuildException: Exception
    {
        public BuildException(string id, string message, Location? location = null)
            : base(message)
        {
            Id = id;
            Location = location;
        }
        
        public string Id { get; }

        public Location? Location { get; }
    }
}