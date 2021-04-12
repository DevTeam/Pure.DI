namespace Pure.DI.Tests.Integration
{
    using Shouldly;
    using Xunit;

    public class PerThreadLifetimeTests
    {
        [Fact]
        public void ShouldSupportPerThread()
        {
            // Given
            var statements =
                "var task1 = System.Threading.Tasks.Task.Run(() => { System.Threading.Thread.Sleep(10); return Composer.Resolve<CompositionRoot>(); });" +
                "var task2 = System.Threading.Tasks.Task.Run(() => { System.Threading.Thread.Sleep(20); return Composer.Resolve<CompositionRoot>(); });" +
                "var task3 = System.Threading.Tasks.Task.Run(() => { System.Threading.Thread.Sleep(30); return (Composer.Resolve<CompositionRoot>(), Composer.Resolve<CompositionRoot>()); });" +
                "System.Threading.Tasks.Task.WaitAll(task1, task2);" +
                "System.Console.WriteLine(task1.Result.EqValue);" +
                "System.Console.WriteLine(task2.Result.EqValue);" +
                "System.Console.WriteLine(task1.Result.Value == task2.Result.Value);" +
                "System.Console.WriteLine(task3.Result.Item1.Value == task3.Result.Item2.Value);";

            // When

            var output = @"
            namespace Sample
            {
                using System;
                using Pure.DI;               

                public class CompositionRoot
                {
                    public readonly bool EqValue;
                    public readonly Foo Value;
                    internal CompositionRoot(Foo value1, Foo value2) 
                    {
                        EqValue = value1 == value2;        
                        Value = value1;
                    }
                }

                public class Foo { }

                internal static partial class Composer
                {
                    static Composer()
                    {
                        DI.Setup()
                            .Bind<Foo>().As(Pure.DI.Lifetime.PerThread).To<Foo>()
                            .Bind<CompositionRoot>().To<CompositionRoot>();
                    }                    
                }    
            }".Run(out var generatedCode, new RunOptions { Statements = statements });

            // Then
            output.Count.ShouldBe(4, generatedCode);
            output[0].ShouldBe("True", generatedCode);
            output[1].ShouldBe("True", generatedCode);
            output[2].ShouldBe("False", generatedCode);
            output[3].ShouldBe("True", generatedCode);
        }

        [Fact]
        public void ShouldSupportPerThreadWhenFactory()
        {
            // Given
            var statements =
                "var task1 = System.Threading.Tasks.Task.Run(() => { System.Threading.Thread.Sleep(10); return Composer.Resolve<CompositionRoot>(); });" +
                "var task2 = System.Threading.Tasks.Task.Run(() => { System.Threading.Thread.Sleep(20); return Composer.Resolve<CompositionRoot>(); });" +
                "System.Threading.Tasks.Task.WaitAll(task1, task2);" +
                "System.Console.WriteLine(task1.Result.EqValue);" +
                "System.Console.WriteLine(task2.Result.EqValue);" +
                "System.Console.WriteLine(task1.Result.Value == task2.Result.Value);";

            // When

            var output = @"
            namespace Sample
            {
                using System;
                using Pure.DI;               

                public class CompositionRoot
                {
                    public readonly bool EqValue;
                    public readonly Foo Value;
                    internal CompositionRoot(Foo value1, Foo value2) 
                    {
                        EqValue = value1 == value2;        
                        Value = value1;
                    }
                }

                public class Foo { }

                internal static partial class Composer
                {
                    static Composer()
                    {
                        DI.Setup()
                            .Bind<Foo>().As(Pure.DI.Lifetime.PerThread).To(ctx => new Foo())
                            .Bind<CompositionRoot>().To<CompositionRoot>();
                    }                    
                }    
            }".Run(out var generatedCode, new RunOptions { Statements = statements });

            // Then
            output.Count.ShouldBe(3, generatedCode);
            output[0].ShouldBe("True", generatedCode);
            output[1].ShouldBe("True", generatedCode);
            output[2].ShouldBe("False", generatedCode);
        }
    }
}