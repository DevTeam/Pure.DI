namespace Pure.DI.Tests
{
    using System.Collections.Generic;

    public class RunOptions
    {
        public string Statements = "System.Console.WriteLine(Composer.Resolve<CompositionRoot>().Value);";
        
        public readonly List<string> AdditionalCode = new();
    }
}