namespace Pure.DI.IntegrationTests;

/// <summary>
/// Integration tests for factory rewriting behaviors.
/// </summary>
public class FactoryRewriterIntegrationTests
{
    [Fact]
    public async Task ShouldIgnoreShadowedContextInNestedLambda()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               class FakeContext
                               {
                                   public void Inject(out int value) => value = 13;
                               }

                               class Service
                               {
                                   public Service(int value) => Value = value;

                                   public int Value { get; }
                               }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind().To<Service>(ctx =>
                                           {
                                               Func<FakeContext, int> createValue = ctx =>
                                               {
                                                   ctx.Inject(out var value);
                                                   return value;
                                               };

                                               var fake = new FakeContext();
                                               return new Service(createValue(fake));
                                           })
                                           .Root<Service>("Root");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var service = composition.Root;
                                       Console.WriteLine(service.Value);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["13"], result);
    }

    [Fact]
    public async Task ShouldSupportDiscardInjection()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDependency { }

                               class Dependency : IDependency
                               {
                                   public static int Count;

                                   public Dependency() => Count++;
                               }

                               class Service { }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDependency>().To<Dependency>()
                                           .Bind().To<Service>(ctx =>
                                           {
                                               ctx.Inject(out IDependency _);
                                               return new Service();
                                           })
                                           .Root<Service>("Root");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var service = composition.Root;
                                       Console.WriteLine(Dependency.Count);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["1"], result);
    }

    // See https://github.com/DevTeam/Pure.DI/issues/152
    // The bracket is missing in the generated code when ctx.Inject is used
    // directly in a switch-case clause (without an explicit block).
    // The expected generated code should be:
    //   case WorkerType.A:
    //   {
    //       global::Sample.WorkerA localWorkerA = new global::Sample.WorkerA();
    //       return localWorkerA;
    //   }
    // instead of the current broken output:
    //   case WorkerType.A:
    //   {
    //       global::Sample.WorkerA localWorkerA = new global::Sample.WorkerA();
    //   }
    //   return localWorkerA;
    [Fact]
    public async Task ShouldSupportInjectInSwitchCaseWithoutBlock()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               enum WorkerType { A, B }

                               interface IWorker { }

                               class WorkerA : IWorker
                               {
                                   public override string ToString() => "A";
                               }

                               class WorkerB : IWorker
                               {
                                   public override string ToString() => "B";
                               }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<WorkerA>().To<WorkerA>()
                                           .Bind<WorkerB>().To<WorkerB>()
                                           .Bind<Func<WorkerType, IWorker>>().To(ctx => new Func<WorkerType, IWorker>(workerType =>
                                           {
                                               switch (workerType)
                                               {
                                                   case WorkerType.A:
                                                       ctx.Inject(out WorkerA workerA);
                                                       return workerA;
                                                   case WorkerType.B:
                                                       ctx.Inject(out WorkerB workerB);
                                                       return workerB;
                                                   default:
                                                       return null!;
                                               }
                                           }))
                                           .Root<Func<WorkerType, IWorker>>("Factory");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var factory = composition.Factory;
                                       Console.WriteLine($"{factory(WorkerType.A)} {factory(WorkerType.B)}");
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["A B"], result);
    }

    // See https://github.com/DevTeam/Pure.DI/issues/152
    // The workaround mentioned in the issue is to wrap case clauses in explicit
    // blocks. This test ensures the workaround actually produces compilable code.
    [Fact]
    public async Task ShouldSupportInjectInSwitchCaseWithBlock()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               enum WorkerType { A, B }

                               interface IWorker { }

                               class WorkerA : IWorker
                               {
                                   public override string ToString() => "A";
                               }

                               class WorkerB : IWorker
                               {
                                   public override string ToString() => "B";
                               }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<WorkerA>().To<WorkerA>()
                                           .Bind<WorkerB>().To<WorkerB>()
                                           .Bind<Func<WorkerType, IWorker>>().To(ctx => new Func<WorkerType, IWorker>(workerType =>
                                           {
                                               switch (workerType)
                                               {
                                                   case WorkerType.A:
                                                   {
                                                       ctx.Inject(out WorkerA workerA);
                                                       return workerA;
                                                   }
                                                   case WorkerType.B:
                                                   {
                                                       ctx.Inject(out WorkerB workerB);
                                                       return workerB;
                                                   }
                                                   default:
                                                       return null!;
                                               }
                                           }))
                                           .Root<Func<WorkerType, IWorker>>("Factory");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var factory = composition.Factory;
                                       Console.WriteLine($"{factory(WorkerType.A)} {factory(WorkerType.B)}");
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["A B"], result);
    }

    // See https://github.com/DevTeam/Pure.DI/issues/152
    // Same root cause as the switch-case scenario: when ctx.Inject is the only
    // statement under `if (...)`, the rewrite wraps it into a Block but a
    // following statement (like `return ...`) would fall outside the scope.
    [Fact]
    public async Task ShouldSupportInjectInIfStatementWithoutBlock()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IWorker { }

                               class WorkerA : IWorker
                               {
                                   public override string ToString() => "A";
                               }

                               class WorkerB : IWorker
                               {
                                   public override string ToString() => "B";
                               }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<WorkerA>().To<WorkerA>()
                                           .Bind<WorkerB>().To<WorkerB>()
                                           .Bind<Func<bool, IWorker>>().To(ctx => new Func<bool, IWorker>(useA =>
                                           {
                                               if (useA)
                                                   ctx.Inject(out WorkerA workerA);
                                               else
                                                   ctx.Inject(out WorkerB workerB);
                                               return useA ? (IWorker)new WorkerA() : new WorkerB();
                                           }))
                                           .Root<Func<bool, IWorker>>("Factory");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var factory = composition.Factory;
                                       Console.WriteLine($"{factory(true)} {factory(false)}");
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["A B"], result);
    }

    // See https://github.com/DevTeam/Pure.DI/issues/152
    // An `else if` chain where ctx.Inject sits under `else if` without an
    // explicit block must still compile.
    [Fact]
    public async Task ShouldSupportInjectInElseIfChainWithoutBlock()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IWorker { }

                               class WorkerA : IWorker
                               {
                                   public override string ToString() => "A";
                               }

                               class WorkerB : IWorker
                               {
                                   public override string ToString() => "B";
                               }

                               class WorkerC : IWorker
                               {
                                   public override string ToString() => "C";
                               }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<WorkerA>().To<WorkerA>()
                                           .Bind<WorkerB>().To<WorkerB>()
                                           .Bind<WorkerC>().To<WorkerC>()
                                           .Bind<Func<int, IWorker>>().To(ctx => new Func<int, IWorker>(key =>
                                           {
                                               if (key == 1)
                                                   ctx.Inject(out WorkerA workerA);
                                               else if (key == 2)
                                                   ctx.Inject(out WorkerB workerB);
                                               else
                                                   ctx.Inject(out WorkerC workerC);
                                               if (key == 1) return new WorkerA();
                                               if (key == 2) return new WorkerB();
                                               return new WorkerC();
                                           }))
                                           .Root<Func<int, IWorker>>("Factory");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var factory = composition.Factory;
                                       Console.WriteLine($"{factory(1)} {factory(2)} {factory(3)}");
                                   }
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["A B C"], result);
    }

    // See https://github.com/DevTeam/Pure.DI/issues/152
    // ctx.BuildUp has the same rewriting path as ctx.Inject, so it must also
    // work inside switch cases without an explicit block.
    [Fact]
    public async Task ShouldSupportBuildUpInSwitchCaseWithoutBlock()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               enum Mode { On, Off }

                               interface IService { }

                               class Service : IService
                               {
                                   public string Status { get; set; } = "off";
                               }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IService>().To<Service>()
                                           .Bind<int>().To(_ => 42)
                                           .Bind<Func<Mode, IService>>().To(ctx => new Func<Mode, IService>(mode =>
                                           {
                                               ctx.Inject(out IService service);
                                               switch (mode)
                                               {
                                                   case Mode.On:
                                                       ctx.BuildUp(service);
                                                       ctx.Inject(out int v);
                                                       ((Service)service).Status = "on:" + v;
                                                       break;
                                                   case Mode.Off:
                                                       ((Service)service).Status = "off";
                                                       break;
                                               }
                                               return service;
                                           }))
                                           .Root<Func<Mode, IService>>("Factory");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var factory = composition.Factory;
                                       Console.WriteLine($"{((Service)factory(Mode.On)).Status} {((Service)factory(Mode.Off)).Status}");
                                   }
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["on:42 off"], result);
    }

    // See https://github.com/DevTeam/Pure.DI/issues/152
    // Same as the switch case scenario, but using the conditional branch of a
    // ternary-like `if`. This makes sure the merged scope works for nested
    // ifs without explicit blocks.
    [Fact]
    public async Task ShouldSupportInjectInNestedIfWithoutBlock()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IWorker { }

                               class WorkerA : IWorker
                               {
                                   public override string ToString() => "A";
                               }

                               class WorkerB : IWorker
                               {
                                   public override string ToString() => "B";
                               }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<WorkerA>().To<WorkerA>()
                                           .Bind<WorkerB>().To<WorkerB>()
                                           .Bind<Func<int, IWorker>>().To(ctx => new Func<int, IWorker>(key =>
                                           {
                                               if (key == 1)
                                                   if (key == 1)
                                                       ctx.Inject(out WorkerA workerA);
                                                   else
                                                       ctx.Inject(out WorkerB workerB);
                                               else
                                                   ctx.Inject(out WorkerB workerB);
                                               return key == 1 ? (IWorker)new WorkerA() : new WorkerB();
                                           }))
                                           .Root<Func<int, IWorker>>("Factory");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var factory = composition.Factory;
                                       Console.WriteLine($"{factory(1)} {factory(2)}");
                                   }
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["A B"], result);
    }

    // See https://github.com/DevTeam/Pure.DI/issues/152
    // The original switch-case issue, but with the more complete case structure
    // (multiple cases, default branch). This is essentially the case from the
    // bug report verbatim, just spelled out as an integration test.
    [Fact]
    public async Task ShouldSupportOriginalBugReportScenario()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace My
                           {
                               public enum WorkerType
                               {
                                   A,
                                   B
                               }

                               public interface IWorker
                               {
                               }

                               public class WorkerA : IWorker
                               {
                                   public override string ToString() => "A";
                               }

                               public class WorkerB : IWorker
                               {
                                   public override string ToString() => "B";
                               }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<WorkerA>().To<WorkerA>()
                                           .Bind<WorkerB>().To<WorkerB>()
                                           .Bind<Func<WorkerType, IWorker>>().To(ctx => new Func<WorkerType, IWorker>(workerType =>
                                           {
                                               switch (workerType)
                                               {
                                                   case WorkerType.A:
                                                       ctx.Inject(out WorkerA workerA);
                                                       return workerA;
                                                   case WorkerType.B:
                                                       ctx.Inject(out WorkerB workerB);
                                                       return workerB;
                                                   default:
                                                       return null!;
                                               }
                                           }))
                                           .Root<Func<WorkerType, IWorker>>("Factory");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var factory = composition.Factory;
                                       Console.WriteLine($"{factory(WorkerType.A)} {factory(WorkerType.B)}");
                                   }
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["A B"], result);
    }

    // See https://github.com/DevTeam/Pure.DI/issues/152
    // Edge case: a switch case that uses ctx.Inject without an explicit
    // block AND uses `break` (not `return`) to exit the switch. The injected
    // variable is consumed by a statement that follows the switch, so the
    // merged scope created by VisitSwitchSection must keep the variable in
    // scope outside the case body.
    [Fact]
    public async Task ShouldSupportInjectInSwitchCaseWithBreakStatement()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               enum WorkerType { A, B }

                               interface IWorker { }

                               class WorkerA : IWorker
                               {
                                   public override string ToString() => "A";
                               }

                               class WorkerB : IWorker
                               {
                                   public override string ToString() => "B";
                               }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<WorkerA>().To<WorkerA>()
                                           .Bind<WorkerB>().To<WorkerB>()
                                           .Bind<Func<WorkerType, IWorker>>().To(ctx => new Func<WorkerType, IWorker>(workerType =>
                                           {
                                               IWorker result;
                                               switch (workerType)
                                               {
                                                   case WorkerType.A:
                                                       ctx.Inject(out WorkerA workerA);
                                                       result = workerA;
                                                       break;
                                                   case WorkerType.B:
                                                       ctx.Inject(out WorkerB workerB);
                                                       result = workerB;
                                                       break;
                                                   default:
                                                       result = null!;
                                                       break;
                                               }
                                               return result;
                                           }))
                                           .Root<Func<WorkerType, IWorker>>("Factory");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var factory = composition.Factory;
                                       Console.WriteLine($"{factory(WorkerType.A)} {factory(WorkerType.B)}");
                                   }
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["A B"], result);
    }

    // See https://github.com/DevTeam/Pure.DI/issues/152
    // Edge case: a switch with an empty `default:` branch alongside a case
    // that uses ctx.Inject without an explicit block. The empty default
    // must remain compilable and the injected variable must stay in scope
    // for the trailing `return`.
    [Fact]
    public async Task ShouldSupportInjectInSwitchWithEmptyDefault()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               enum WorkerType { A, B, Other }

                               interface IWorker { }

                               class WorkerA : IWorker
                               {
                                   public override string ToString() => "A";
                               }

                               class WorkerB : IWorker
                               {
                                   public override string ToString() => "B";
                               }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<WorkerA>().To<WorkerA>()
                                           .Bind<WorkerB>().To<WorkerB>()
                                           .Bind<Func<WorkerType, IWorker>>().To(ctx => new Func<WorkerType, IWorker>(workerType =>
                                           {
                                               switch (workerType)
                                               {
                                                   case WorkerType.A:
                                                       ctx.Inject(out WorkerA workerA);
                                                       return workerA;
                                                   case WorkerType.B:
                                                       ctx.Inject(out WorkerB workerB);
                                                       return workerB;
                                                   default:
                                                       break;
                                               }
                                               return null!;
                                           }))
                                           .Root<Func<WorkerType, IWorker>>("Factory");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var factory = composition.Factory;
                                       Console.WriteLine($"{factory(WorkerType.A)} {factory(WorkerType.B)} {factory(WorkerType.Other) is null}");
                                   }
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["A B True"], result);
    }

    // See https://github.com/DevTeam/Pure.DI/issues/152
    // Edge case: ctx.Override and ctx.Let inside a switch case without an
    // explicit block. These calls do not declare a local variable, but they
    // still go through VisitExpressionStatement, so VisitSwitchSection
    // should not break them.
    [Fact]
    public async Task ShouldSupportOverrideInSwitchCaseWithoutBlock()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               enum WorkerType { A, B }

                               interface IService
                               {
                                   string Greeting { get; }
                               }

                               class Service : IService
                               {
                                   public string Greeting { get; set; } = "";
                               }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IService>().To<Service>()
                                           .Bind<Func<WorkerType, IService>>().To(ctx => new Func<WorkerType, IService>(workerType =>
                                           {
                                               ctx.Inject(out IService service);
                                               switch (workerType)
                                               {
                                                   case WorkerType.A:
                                                       ctx.Override("hello A");
                                                       break;
                                                   case WorkerType.B:
                                                       ctx.Override("hello B");
                                                       break;
                                               }
                                               ((Service)service).Greeting = "default";
                                               return service;
                                           }))
                                           .Root<Func<WorkerType, IService>>("Factory");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var factory = composition.Factory;
                                       Console.WriteLine($"{factory(WorkerType.A).Greeting} {factory(WorkerType.B).Greeting}");
                                   }
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["default default"], result);
    }

    // See https://github.com/DevTeam/Pure.DI/issues/152
    // Edge case: a switch case that uses ctx.Inject without an explicit
    // block AND whose enclosing lambda body ends with a `return ... ? ... : ...`
    // ternary. The injected variable must be visible to the ternary
    // expression in the trailing `return`.
    [Fact]
    public async Task ShouldSupportSwitchCaseWithTernaryInReturn()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               enum WorkerType { A, B }

                               interface IWorker { }

                               class WorkerA : IWorker
                               {
                                   public override string ToString() => "A";
                               }

                               class WorkerB : IWorker
                               {
                                   public override string ToString() => "B";
                               }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<WorkerA>().To<WorkerA>()
                                           .Bind<WorkerB>().To<WorkerB>()
                                           .Bind<Func<WorkerType, IWorker>>().To(ctx => new Func<WorkerType, IWorker>(workerType =>
                                           {
                                               IWorker fallback = null!;
                                               switch (workerType)
                                               {
                                                   case WorkerType.A:
                                                       ctx.Inject(out WorkerA workerA);
                                                       fallback = workerA;
                                                       break;
                                                   case WorkerType.B:
                                                       ctx.Inject(out WorkerB workerB);
                                                       fallback = workerB;
                                                       break;
                                               }
                                               return workerType == WorkerType.A ? (IWorker)new WorkerA() : fallback;
                                           }))
                                           .Root<Func<WorkerType, IWorker>>("Factory");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var factory = composition.Factory;
                                       Console.WriteLine($"{factory(WorkerType.A)} {factory(WorkerType.B)}");
                                   }
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["A B"], result);
    }

    // See https://github.com/DevTeam/Pure.DI/issues/152
    // Edge case: a switch case whose body is already a single explicit
    // block statement containing ctx.Inject. The scope merge logic must
    // not corrupt the existing block layout.
    [Fact]
    public async Task ShouldSupportInjectInSwitchCaseWithSingleStatementBlock()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               enum WorkerType { A, B }

                               interface IWorker { }

                               class WorkerA : IWorker
                               {
                                   public override string ToString() => "A";
                               }

                               class WorkerB : IWorker
                               {
                                   public override string ToString() => "B";
                               }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<WorkerA>().To<WorkerA>()
                                           .Bind<WorkerB>().To<WorkerB>()
                                           .Bind<Func<WorkerType, IWorker>>().To(ctx => new Func<WorkerType, IWorker>(workerType =>
                                           {
                                               IWorker result;
                                               switch (workerType)
                                               {
                                                   case WorkerType.A:
                                                   {
                                                       ctx.Inject(out WorkerA workerA);
                                                       result = workerA;
                                                       break;
                                                   }
                                                   case WorkerType.B:
                                                   {
                                                       ctx.Inject(out WorkerB workerB);
                                                       result = workerB;
                                                       break;
                                                   }
                                                   default:
                                                       result = null!;
                                                       break;
                                               }
                                               return result;
                                           }))
                                           .Root<Func<WorkerType, IWorker>>("Factory");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var factory = composition.Factory;
                                       Console.WriteLine($"{factory(WorkerType.A)} {factory(WorkerType.B)}");
                                   }
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["A B"], result);
    }

    // See https://github.com/DevTeam/Pure.DI/issues/152
    // Edge case: nested switch statements where ctx.Inject is inside an inner
    // case without an explicit block. The scope merge must work recursively
    // through nested switches.
    [Fact]
    public async Task ShouldSupportInjectInNestedSwitchWithoutBlock()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               enum OuterKind { First, Second }
                               enum InnerKind { A, B }

                               interface IWorker { }

                               class WorkerA : IWorker
                               {
                                   public override string ToString() => "A";
                               }

                               class WorkerB : IWorker
                               {
                                   public override string ToString() => "B";
                               }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<WorkerA>().To<WorkerA>()
                                           .Bind<WorkerB>().To<WorkerB>()
                                           .Bind<Func<OuterKind, InnerKind, IWorker>>().To(ctx => new Func<OuterKind, InnerKind, IWorker>((outer, inner) =>
                                           {
                                               IWorker result;
                                               switch (outer)
                                               {
                                                   case OuterKind.First:
                                                       switch (inner)
                                                       {
                                                           case InnerKind.A:
                                                               ctx.Inject(out WorkerA workerA);
                                                               result = workerA;
                                                               break;
                                                           case InnerKind.B:
                                                               ctx.Inject(out WorkerB workerB);
                                                               result = workerB;
                                                               break;
                                                           default:
                                                               result = null!;
                                                               break;
                                                       }
                                                       break;
                                                   default:
                                                       result = null!;
                                                       break;
                                               }
                                               return result;
                                           }))
                                           .Root<Func<OuterKind, InnerKind, IWorker>>("Factory");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var factory = composition.Factory;
                                       Console.WriteLine($"{factory(OuterKind.First, InnerKind.A)} {factory(OuterKind.First, InnerKind.B)} {factory(OuterKind.Second, InnerKind.A) is null}");
                                   }
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["A B True"], result);
    }

    // See https://github.com/DevTeam/Pure.DI/issues/152
    // ctx.Inject inside a single-statement `for` body (no explicit braces)
    // must compile and execute the inject call for each loop iteration.
    [Fact]
    public async Task ShouldSupportInjectInForLoopWithoutBlock()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDep { }

                               class Dep : IDep
                               {
                                   public static int Count;
                                   public Dep() => Count++;
                               }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDep>().To<Dep>()
                                           .Bind<Func<int, int>>().To(ctx => new Func<int, int>(count =>
                                           {
                                               Dep.Count = 0;
                                               for (int i = 0; i < count; i++)
                                                   ctx.Inject(out IDep _);
                                               return Dep.Count;
                                           }))
                                           .Root<Func<int, int>>("Factory");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var factory = composition.Factory;
                                       Console.WriteLine(factory(3));
                                   }
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["3"], result);
    }

    // See https://github.com/DevTeam/Pure.DI/issues/152
    // ctx.Inject inside a single-statement `while` body (no explicit braces)
    // must compile and execute the inject call for each loop iteration.
    [Fact]
    public async Task ShouldSupportInjectInWhileLoopWithoutBlock()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDep { }

                               class Dep : IDep
                               {
                                   public static int Count;
                                   public Dep() => Count++;
                               }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDep>().To<Dep>()
                                           .Bind<Func<int, int>>().To(ctx => new Func<int, int>(count =>
                                           {
                                               Dep.Count = 0;
                                               int i = 0;
                                               while (i++ < count)
                                                   ctx.Inject(out IDep _);
                                               return Dep.Count;
                                           }))
                                           .Root<Func<int, int>>("Factory");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var factory = composition.Factory;
                                       Console.WriteLine(factory(4));
                                   }
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["4"], result);
    }

    // See https://github.com/DevTeam/Pure.DI/issues/152
    // ctx.Inject inside a single-statement `foreach` body (no explicit braces)
    // must compile.
    [Fact]
    public async Task ShouldSupportInjectInForEachLoopWithoutBlock()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using System.Collections.Generic;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDep { }

                               class Dep : IDep
                               {
                                   public static int Count;
                                   public Dep() => Count++;
                               }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDep>().To<Dep>()
                                           .Bind<Func<IEnumerable<int>, int>>().To(ctx => new Func<IEnumerable<int>, int>(items =>
                                           {
                                               Dep.Count = 0;
                                               foreach (var item in items)
                                                   ctx.Inject(out IDep d);
                                               return Dep.Count;
                                           }))
                                           .Root<Func<IEnumerable<int>, int>>("Factory");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var factory = composition.Factory;
                                       Console.WriteLine(factory(new[] { 1, 2, 3, 4, 5 }));
                                   }
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["5"], result);
    }

    // See https://github.com/DevTeam/Pure.DI/issues/152
    // ctx.Inject inside a single-statement `do-while` body (no explicit braces)
    // must compile.
    [Fact]
    public async Task ShouldSupportInjectInDoWhileLoopWithoutBlock()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDep { }

                               class Dep : IDep
                               {
                                   public static int Count;
                                   public Dep() => Count++;
                               }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDep>().To<Dep>()
                                           .Bind<Func<int, int>>().To(ctx => new Func<int, int>(count =>
                                           {
                                               Dep.Count = 0;
                                               int i = 0;
                                               do
                                                   ctx.Inject(out IDep _);
                                               while (++i < count);
                                               return Dep.Count;
                                           }))
                                           .Root<Func<int, int>>("Factory");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var factory = composition.Factory;
                                       Console.WriteLine(factory(2));
                                   }
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["2"], result);
    }

    // See https://github.com/DevTeam/Pure.DI/issues/152
    // ctx.Inject inside a single-statement `using` declaration body is not valid
    // C# (using declarations have their own scope). But ctx.Inject after a using
    // declaration must still compile.
    [Fact]
    public async Task ShouldSupportInjectAfterUsingDeclaration()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDep { }

                               class Dep : IDep, IDisposable
                               {
                                   public static int Count;
                                   public Dep() => Count++;
                                   public void Dispose() { }
                               }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDep>().To<Dep>()
                                           .Bind<Func<int>>().To(ctx => new Func<int>(() =>
                                           {
                                               Dep.Count = 0;
                                               using var scope = new Dep();
                                               ctx.Inject(out IDep _);
                                               return Dep.Count;
                                           }))
                                           .Root<Func<int>>("Factory");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var factory = composition.Factory;
                                       Console.WriteLine(factory());
                                   }
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["2"], result);
    }

    // See https://github.com/DevTeam/Pure.DI/issues/152
    // ctx.Inject inside a single-statement `lock` body (no explicit braces)
    // must compile.
    [Fact]
    public async Task ShouldSupportInjectInLockStatementWithoutBlock()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               interface IDep { }

                               class Dep : IDep
                               {
                                   public static int Count;
                                   public Dep() => Count++;
                               }

                               static class Setup
                               {
                                   private static void SetupComposition()
                                   {
                                       DI.Setup("Composition")
                                           .Bind<IDep>().To<Dep>()
                                           .Bind<Func<int>>().To(ctx => new Func<int>(() =>
                                           {
                                               Dep.Count = 0;
                                               object syncRoot = new object();
                                               lock (syncRoot)
                                                   ctx.Inject(out IDep _);
                                               return Dep.Count;
                                           }))
                                           .Root<Func<int>>("Factory");
                                   }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       var factory = composition.Factory;
                                       Console.WriteLine(factory());
                                   }
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion.CSharp10));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["1"], result);
    }
}
