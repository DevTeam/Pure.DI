namespace Pure.DI.IntegrationTests;

using System.Collections.Immutable;
using System.Text;
using Core;
using Core.Models;

[Collection(nameof(NonParallelTestsCollectionDefinition))]
public class GraphTests
{
    [Fact]
    public async Task ShouldSupportBindToImplementation()
    {
        // Given

        // When
        var result = await """
namespace Sample
{
    using System;
    using Pure.DI;
    using Sample;

    internal interface IService
    {
    }

    internal interface IDependency
    {
    }

    internal class Service : IService
    {
        internal Service(IDependency dependency)
        {
        }
    }

    internal class Dependency : IDependency
    {
        public Dependency() {}
    }

    internal partial class Composer
    {                   
        private static void Setup() => Pure.DI.DI.Setup("Composer")
            .Bind<IDependency>().To<Dependency>()
            .Bind<IService>().To<Service>(); 
    }               

    public class Program { public static void Main() { } }
}
""".RunAsync();

        // Then
        result.Success.ShouldBeTrue(result.GeneratedCode);
        var graphs = GetGraphs(result);
        graphs.Length.ShouldBe(1, result.GeneratedCode);
        graphs[0].ConvertToString().ShouldBe("""
Dependency()
Service(Sample.IDependency dependency<--Sample.IDependency))
  +[Service(Sample.IDependency dependency<--Sample.IDependency))]<--[Sample.IDependency]--[Dependency()]
""");
    }
    
    [Fact]
    public async Task ShouldSupportRoot()
    {
        // Given

        // When
        var result = await """
namespace Sample
{
    using System;
    using Pure.DI;
    using Sample;

    internal interface IService
    {
    }

    internal interface IDependency
    {
    }

    internal class Service : IService
    {
        internal Service(IDependency dependency)
        {
        }
    }

    internal class Dependency : IDependency
    {
        public Dependency() {}
    }

    internal partial class Composer
    {                   
        private static void Setup() => Pure.DI.DI.Setup("Composer")
            .Bind<IDependency>().To<Dependency>()
            .Bind<IService>().To<Service>()
            .Root<IService>("MyService"); 
    }               

    public class Program { public static void Main() { } }
}
""".RunAsync();

        // Then
        result.Success.ShouldBeTrue(result.GeneratedCode);
        var graphs = GetGraphs(result);
        graphs.Length.ShouldBe(1, result.GeneratedCode);
        graphs[0].ConvertToString().ShouldBe("""
Sample.IService() MyService
  +[Sample.IService() MyService]<--[Sample.IService]--[Service(Sample.IDependency dependency<--Sample.IDependency))]
Dependency()
Service(Sample.IDependency dependency<--Sample.IDependency))
  +[Service(Sample.IDependency dependency<--Sample.IDependency))]<--[Sample.IDependency]--[Dependency()]
""");
    }
    
    [Fact]
    public async Task ShouldSupportBindToImplementationWhenSeveralConstructors()
    {
        // Given

        // When
        var result = await """
namespace Sample
{
    using System;
    using Pure.DI;
    using Sample;

    internal interface IService
    {
    }

    internal interface IDependency
    {
    }

    internal class Service : IService
    {
        internal Service(IDependency dependency)
        {
        }

        public Service()
        {
        }
    }

    internal class Dependency : IDependency
    {
        public Dependency() {}

        public Dependency(int val) {}
    }

    internal partial class Composer
    {                   
        private static void Setup() => Pure.DI.DI.Setup("Composer")
            .Bind<IDependency>().To<Dependency>()
            .Bind<IService>().To<Service>(); 
    }               

    public class Program { public static void Main() { } }
}
""".RunAsync();

        // Then
        result.Success.ShouldBeTrue(result.GeneratedCode);
        var graphs = GetGraphs(result);
        graphs.Length.ShouldBe(1, result.GeneratedCode);
        graphs[0].ConvertToString().ShouldBe("""
Dependency()
Service(Sample.IDependency dependency<--Sample.IDependency))
  +[Service(Sample.IDependency dependency<--Sample.IDependency))]<--[Sample.IDependency]--[Dependency()]
""");
    }
    
    [Fact]
    public async Task ShouldSupportBindToFactory()
    {
        // Given

        // When
        var result = await """
namespace Sample
{
    using System;
    using Pure.DI;
    using Sample;

    internal interface IService
    {
    }

    internal interface IDependency
    {
    }

    internal class Service : IService
    {
        internal Service(IDependency dependency)
        {
        }
    }

    internal class Dependency : IDependency
    {
        public Dependency() {}
    }

    internal partial class Composer
    {                   
        private static void Setup() => Pure.DI.DI.Setup("Composer")
            .Bind<IDependency>().To(_ => new Dependency())
            .Bind<IService>().To<Service>(); 
    }               

    public class Program { public static void Main() { } }
}
""".RunAsync();

        // Then
        result.Success.ShouldBeTrue(result.GeneratedCode);
        var graphs = GetGraphs(result);
        graphs.Length.ShouldBe(1, result.GeneratedCode);
        graphs[0].ConvertToString().ShouldBe("""
new Dependency()
Service(Sample.IDependency dependency<--Sample.IDependency))
  +[Service(Sample.IDependency dependency<--Sample.IDependency))]<--[Sample.IDependency]--[new Dependency()]
""");
    }
    
    [Fact]
    public async Task ShouldSupportBindToArg()
    {
        // Given

        // When
        var result = await """
namespace Sample
{
    using System;
    using Pure.DI;
    using Sample;

    internal interface IService
    {
    }

    internal interface IDependency
    {
    }

    internal class Service : IService
    {
        internal Service(IDependency dependency)
        {
        }
    }

    internal class Dependency : IDependency
    {
        public Dependency() {}
    }

    internal partial class Composer
    {                   
        private static void Setup() => Pure.DI.DI.Setup("Composer")
            .Arg<IDependency>("dep")
            .Bind<IService>().To<Service>(); 
    }

    public class Program { public static void Main() { } }               
}
""".RunAsync();

        // Then
        result.Success.ShouldBeTrue(result.GeneratedCode);
        var graphs = GetGraphs(result);
        graphs.Length.ShouldBe(1, result.GeneratedCode);
        graphs[0].ConvertToString().ShouldBe("""
Sample.IDependency dep
Service(Sample.IDependency dependency<--Sample.IDependency))
  +[Service(Sample.IDependency dependency<--Sample.IDependency))]<--[Sample.IDependency]--[Sample.IDependency dep]
""");
    }
    
    [Fact]
    public async Task ShouldSupportSeparatedSetup()
    {
        // Given

        // When
        var result = await """
namespace Sample
{
    using System;
    using Pure.DI;
    using Sample;

    internal interface IService
    {
    }

    internal interface IDependency
    {
    }

    internal class Service : IService
    {
        internal Service(IDependency dependency)
        {
        }
    }

    internal class Dependency : IDependency
    {
        public Dependency() {}
    }

    internal partial class Composer
    {                   
        private static void Setup()
        {
             Pure.DI.DI.Setup("Composer")
               .Bind<IDependency>().To<Dependency>();

            Pure.DI.DI.Setup("Composer")
                .Bind<IService>().To<Service>();
        }  
    }               

    public class Program { public static void Main() { } }
}
""".RunAsync();

        // Then
        result.Success.ShouldBeTrue(result.GeneratedCode);
        var graphs = GetGraphs(result);
        graphs.Length.ShouldBe(1, result.GeneratedCode);
        graphs[0].ConvertToString().ShouldBe("""
Dependency()
Service(Sample.IDependency dependency<--Sample.IDependency))
  +[Service(Sample.IDependency dependency<--Sample.IDependency))]<--[Sample.IDependency]--[Dependency()]
""");
    }
    
    [Fact]
    public async Task ShouldSupportUnresolvedDependency()
    {
        // Given

        // When
        var result = await """
namespace Sample
{
    using System;
    using Pure.DI;
    using Sample;

    internal interface IService
    {
    }

    internal interface IDependency
    {
    }

    internal class Service : IService
    {
        internal Service(IDependency dependency)
        {
        }
    }

    internal class Dependency : IDependency
    {
        public Dependency(string abc) {}
    }

    internal partial class Composer
    {                   
        private static void Setup() => Pure.DI.DI.Setup("Composer")
            .Bind<IDependency>().To<Dependency>()
            .Bind<IService>().To<Service>(); 
    }

    public class Program { public static void Main() { } }               
}
""".RunAsync();

        // Then
        result.Success.ShouldBeFalse(result.GeneratedCode);
        var graphs = GetGraphs(result);
        graphs.Length.ShouldBe(1, result.GeneratedCode);
        graphs[0].ConvertToString().ShouldBe("""
Dependency(string abc<--string))
  -[Dependency(string abc<--string))]<--[string]--[unresolved]
Service(Sample.IDependency dependency<--Sample.IDependency))
  -[Service(Sample.IDependency dependency<--Sample.IDependency))]<--[Sample.IDependency]--[unresolved]
""");

        var errors = result.Logs.Where(i => i.Id == LogId.ErrorUnresolved).ToImmutableArray();
        errors.Length.ShouldBe(2);
    }
    
    [Fact]
    public async Task ShouldSupportDependsOnWhenInternal()
    {
        // Given

        // When
        var result = await """
namespace Sample
{
    using System;
    using Pure.DI;
    using Sample;

    internal interface IService
    {
    }

    internal interface IDependency
    {
    }

    internal class Service : IService
    {
        internal Service(IDependency dependency)
        {
        }
    }

    internal class Dependency : IDependency
    {
        public Dependency() {}
    }

    internal partial class Composer
    {                   
        private static void Setup1() => Pure.DI.DI.Setup("BaseComposer", ComposerKind.Internal)
            .Bind<IDependency>().To<Dependency>(); 

        private static void Setup2() => Pure.DI.DI.Setup("Composer")
            .DependsOn("BaseComposer")
            .Bind<IService>().To<Service>();
    }

    public class Program { public static void Main() { } }               
}
""".RunAsync();

        // Then
        result.Success.ShouldBeTrue(result.GeneratedCode);
        var graphs = GetGraphs(result);
        graphs.Length.ShouldBe(1, result.GeneratedCode);
        graphs[0].ConvertToString().ShouldBe("""
Dependency()
Service(Sample.IDependency dependency<--Sample.IDependency))
  +[Service(Sample.IDependency dependency<--Sample.IDependency))]<--[Sample.IDependency]--[Dependency()]
""");
    }
    
    [Fact]
    public async Task ShouldSupportDependsOnWhenGlobal()
    {
        // Given

        // When
        var result = await """
namespace Sample
{
    using System;
    using Pure.DI;
    using Sample;

    internal interface IService
    {
    }

    internal interface IDependency
    {
    }

    internal class Service : IService
    {
        internal Service(IDependency dependency)
        {
        }
    }

    internal class Dependency : IDependency
    {
        public Dependency() {}
    }

    internal partial class Composer
    {                   
        private static void Setup1() => Pure.DI.DI.Setup("BaseComposer", ComposerKind.Global)
            .Bind<IDependency>().To<Dependency>(); 

        private static void Setup2() => Pure.DI.DI.Setup("Composer")
            .Bind<IService>().To<Service>();
    }

    public class Program { public static void Main() { } }               
}
""".RunAsync();

        // Then
        result.Success.ShouldBeTrue(result.GeneratedCode);
        var graphs = GetGraphs(result);
        graphs.Length.ShouldBe(1, result.GeneratedCode);
        graphs[0].ConvertToString().ShouldBe("""
Dependency()
Service(Sample.IDependency dependency<--Sample.IDependency))
  +[Service(Sample.IDependency dependency<--Sample.IDependency))]<--[Sample.IDependency]--[Dependency()]
""");
    }
    
    [Fact]
    public async Task ShouldSupportDependsOnWhenPublic()
    {
        // Given

        // When
        var result = await """
namespace Sample
{
    using System;
    using Pure.DI;
    using Sample;

    internal interface IService
    {
    }

    internal interface IDependency
    {
    }

    internal class Service : IService
    {
        internal Service(IDependency dependency)
        {
        }
    }

    internal class Dependency : IDependency
    {
        public Dependency() {}
    }

    internal partial class Composer
    {                   
        private static void Setup1() => Pure.DI.DI.Setup("BaseComposer", ComposerKind.Public)
            .Bind<IDependency>().To<Dependency>(); 

        private static void Setup2() => Pure.DI.DI.Setup("Composer")
            .DependsOn("BaseComposer")
            .Bind<IService>().To<Service>();
    }

    public class Program { public static void Main() { } }               
}
""".RunAsync();

        // Then
        result.Success.ShouldBeTrue(result.GeneratedCode);
        var graphs = GetGraphs(result);
        graphs.Length.ShouldBe(2, result.GeneratedCode);
        graphs[1].ConvertToString().ShouldBe("""
Dependency()
Service(Sample.IDependency dependency<--Sample.IDependency))
  +[Service(Sample.IDependency dependency<--Sample.IDependency))]<--[Sample.IDependency]--[Dependency()]
""");
    }
    
    [Theory]
    [InlineData(5)]
    [InlineData(300)]
    public async Task ShouldSupportLongSetup(int count)
    {
        // Given
        var graphText = new StringBuilder();
        var bindingCode = new StringBuilder();
        bindingCode.AppendLine();
        for (var i = 0; i <= count; i++)
        {
            bindingCode.AppendLine($"                        .Bind<IDependency{i}>().To<Dependency{i}>()");
        }

        bindingCode.AppendLine("                    ;");
        bindingCode.AppendLine("                }");
        for (var i = 0; i < count; i++)
        {
            bindingCode.AppendLine($"                internal interface IDependency{i} {{}}");
            bindingCode.AppendLine($"                internal class Dependency{i} : IDependency{i} {{ public Dependency{i}(IDependency{i + 1} dep) {{}} }}");
            bindingCode.AppendLine();
        }
        
        bindingCode.AppendLine($"                internal interface IDependency{count} {{}}");
        bindingCode.AppendLine($"                internal class Dependency{count} : IDependency{count} {{ public Dependency{count}() {{}} }}");
        bindingCode.AppendLine("            }");

        graphText.AppendLine();
        for (var i = 0; i < count - 1; i++)
        {
            graphText.AppendLine($"Dependency{i}(Sample.IDependency{i + 1} dep<--Sample.IDependency{i + 1}))");
            graphText.AppendLine($"  +[Dependency{i}(Sample.IDependency{i + 1} dep<--Sample.IDependency{i + 1}))]<--[Sample.IDependency{i + 1}]--[Dependency{i + 1}(Sample.IDependency{i + 2} dep<--Sample.IDependency{i + 2}))]");
        }

        graphText.AppendLine($"Dependency{count-1}(Sample.IDependency{count} dep<--Sample.IDependency{count}))");
        graphText.AppendLine($"  +[Dependency{count-1}(Sample.IDependency{count} dep<--Sample.IDependency{count}))]<--[Sample.IDependency{count}]--[Dependency{count}()]");
        graphText.Append($"Dependency{count}()");
        
        var setupCode = """
namespace Sample
{
    using System;
    using Pure.DI;
    using Sample;

    public class Program { public static void Main() { } }

    internal interface IService
    {
    }

    internal class Service : IService
    {
        internal Service(IDependency0 dependency)
        {
        }
    }

    internal partial class Composer
    {                   
        private static void Setup() => Pure.DI.DI.Setup("Composer")
            .Bind<IService>().To<Service>() 
""" + bindingCode;

        // When
        var result = await setupCode.RunAsync();

        // Then
        result.Success.ShouldBeTrue(result.GeneratedCode);
        var graphs = GetGraphs(result);
        graphs.Length.ShouldBe(1, result.GeneratedCode);
        graphs[0].ConvertToString().ShouldBe("""
Service(Sample.IDependency0 dependency<--Sample.IDependency0))
  +[Service(Sample.IDependency0 dependency<--Sample.IDependency0))]<--[Sample.IDependency0]--[Dependency0(Sample.IDependency1 dep<--Sample.IDependency1))]
""" + graphText);
    }
    
    [Fact]
    public async Task ShouldSupportBindToImplementationWhenSeveralConstructorsAndHasUnresolvedOne()
    {
        // Given

        // When
        var result = await """
namespace Sample
{
    using System;
    using Pure.DI;
    using Sample;

    internal interface IService
    {
    }

    internal interface IDependency
    {
    }

    internal class Service : IService
    {
        internal Service(IDependency dependency)
        {
        }
    }

    internal class Dependency : IDependency
    {
        public Dependency(string abc) {}

        public Dependency() {}
    }

    internal partial class Composer
    {                   
        private static void Setup() => Pure.DI.DI.Setup("Composer")
            .Bind<IDependency>().To<Dependency>()
            .Bind<IService>().To<Service>(); 
    }

    public class Program { public static void Main() { } }               
}
""".RunAsync();

        // Then
        result.Success.ShouldBeTrue(result.GeneratedCode);
        var graphs = GetGraphs(result);
        graphs.Length.ShouldBe(1, result.GeneratedCode);
        graphs[0].ConvertToString().ShouldBe("""
Dependency()
Service(Sample.IDependency dependency<--Sample.IDependency))
  +[Service(Sample.IDependency dependency<--Sample.IDependency))]<--[Sample.IDependency]--[Dependency()]
""");
    }
    
    [Fact]
    public async Task ShouldSupportBindToGenericImplementation()
    {
        // Given

        // When
        var result = await """
namespace Sample
{
    using System;
    using Pure.DI;
    using Sample;

    internal interface IService
    {
    }

    internal interface IDependency<T>
    {
    }

    internal class Service : IService
    {
        internal Service(IDependency<int> dependency)
        {
        }
    }

    internal class Dependency<T> : IDependency<T>
    {
        public Dependency() {}
    }

    internal partial class Composer
    {                   
        private static void Setup() => Pure.DI.DI.Setup("Composer")
            .Bind<IDependency<TT>>().To<Dependency<TT>>()
            .Bind<IService>().To<Service>(); 
    }

    public class Program { public static void Main() { } }               
}
""".RunAsync();

        // Then
        result.Success.ShouldBeTrue(result.GeneratedCode);
        var graphs = GetGraphs(result);
        graphs.Length.ShouldBe(1, result.GeneratedCode);
        graphs[0].ConvertToString().ShouldBe("""
Service(Sample.IDependency<int> dependency<--Sample.IDependency<int>))
  +[Service(Sample.IDependency<int> dependency<--Sample.IDependency<int>))]<--[Sample.IDependency<int>]--[Dependency<int>()]
Dependency<int>()
""");
    }
    
    [Fact]
    public async Task ShouldSupportBindToGenericFactory()
    {
        // Given

        // When
        var result = await """
namespace Sample
{
    using System;
    using Pure.DI;
    using Sample;

    internal interface IService
    {
    }

    internal interface IDependency<T>
    {
    }

    internal class Service : IService
    {
        internal Service(IDependency<int> dependency)
        {
        }
    }

    internal class Dependency<T> : IDependency<T>
    {
        public Dependency(T[] arr) {}
    }

    internal partial class Composer
    {                   
        private static void Setup() => Pure.DI.DI.Setup("Composer")
            .Bind<IDependency<TT>>().To(_ => new Dependency<TT>(new TT[1]))
            .Bind<IService>().To<Service>(); 
    }

    public class Program { public static void Main() { } }               
}
""".RunAsync();

        // Then
        result.Success.ShouldBeTrue(result.GeneratedCode);
        var graphs = GetGraphs(result);
        graphs.Length.ShouldBe(1, result.GeneratedCode);
        graphs[0].ConvertToString().ShouldBe("""
Service(Sample.IDependency<int> dependency<--Sample.IDependency<int>))
  +[Service(Sample.IDependency<int> dependency<--Sample.IDependency<int>))]<--[Sample.IDependency<int>]--[new Dependency<int>(new int[1])]
new Dependency<int>(new int[1])
""");
    }
    
    [Fact]
    public async Task ShouldSupportBindToComplexGenericFactory()
    {
        // Given

        // When
        var result = await """
namespace Sample
{
    using System;
    using Pure.DI;
    using Sample;

    internal interface IService
    {
    }

    internal interface IDependency<T>
    {
    }

    internal class Service : IService
    {
        internal Service(IDependency<int> dependency)
        {
        }
    }

    internal class Dependency<T> : IDependency<T>
    {
        public Dependency(T[] arr, string str) {}
    }

    internal partial class Composer
    {                   
        private static void Setup() => Pure.DI.DI.Setup("Composer")
            .Bind<string>("MyStr").To(_ => "Abc")                        
            .Bind<IDependency<TT>>().To(ctx => {
                ctx.Inject<TT[]>(out var array); 
                ctx.Inject<string>("MyStr", out var str);
                return new Dependency<TT>(array, str);
            })
            .Bind<IService>().To<Service>()
            .Bind<int[]>().To(_ => new int[] {1, 2, 3}); 
    }

    public class Program { public static void Main() { } }               
}
""".RunAsync();

        // Then
        result.Success.ShouldBeTrue(result.GeneratedCode);
        var graphs = GetGraphs(result);
        graphs.Length.ShouldBe(1, result.GeneratedCode);
        graphs[0].ConvertToString().ShouldBe("""
"Abc"
Service(Sample.IDependency<int> dependency<--Sample.IDependency<int>))
  +[Service(Sample.IDependency<int> dependency<--Sample.IDependency<int>))]<--[Sample.IDependency<int>]--[{
                ctx.Inject<int[]>(out int[]array); 
                ctx.Inject<string>("MyStr", out var str);
                return new Dependency<int>(array, str);
            }]
new int[] {1, 2, 3}
{
                ctx.Inject<int[]>(out int[]array); 
                ctx.Inject<string>("MyStr", out var str);
                return new Dependency<int>(array, str);
            }
  +[{
                ctx.Inject<int[]>(out int[]array); 
                ctx.Inject<string>("MyStr", out var str);
                return new Dependency<int>(array, str);
            }]<--[int[]]--[new int[] {1, 2, 3}]
  +[{
                ctx.Inject<int[]>(out int[]array); 
                ctx.Inject<string>("MyStr", out var str);
                return new Dependency<int>(array, str);
            }]<--[string(MyStr)]--["Abc"]
""");
    }
    
    [Fact]
    public async Task ShouldSupportBindToComplexGenericImplementation()
    {
        // Given

        // When
        var result = await """
namespace Sample
{
    using System;
    using System.Collections.Generic;
    using Pure.DI;
    using Sample;

    internal interface IService
    {
    }

    internal class Dependency2
    {
    }

    internal interface IDependency<T1, T2>
    {
    }

    internal class Service : IService
    {
        internal Service(IDependency<IDictionary<int, double>, IList<string>> dependency)
        {
        }
    }

    internal class Dependency<T1, T2> : IDependency<T1, T2>
    {
        public Dependency(int val) {}

        public Dependency() {}

        public Dependency(Dependency2 dep) {}
    }

    internal partial class Composer
    {                   
        private static void Setup() => Pure.DI.DI.Setup("Composer")
            .Bind<IDependency<TT1, TT2>>().To<Dependency<TT1, TT2>>()
            .Bind<IService>().To<Service>(); 
    }

    public class Program { public static void Main() { } }               
}
""".RunAsync();

        // Then
        result.Success.ShouldBeTrue(result.GeneratedCode);
        var graphs = GetGraphs(result);
        graphs.Length.ShouldBe(1, result.GeneratedCode);
        graphs[0].ConvertToString().ShouldBe("""
Service(Sample.IDependency<System.Collections.Generic.IDictionary<int, double>, System.Collections.Generic.IList<string>> dependency<--Sample.IDependency<System.Collections.Generic.IDictionary<int, double>, System.Collections.Generic.IList<string>>))
  +[Service(Sample.IDependency<System.Collections.Generic.IDictionary<int, double>, System.Collections.Generic.IList<string>> dependency<--Sample.IDependency<System.Collections.Generic.IDictionary<int, double>, System.Collections.Generic.IList<string>>))]<--[Sample.IDependency<System.Collections.Generic.IDictionary<int, double>, System.Collections.Generic.IList<string>>]--[Dependency<System.Collections.Generic.IDictionary<int, double>, System.Collections.Generic.IList<string>>(Sample.Dependency2 dep<--Sample.Dependency2))]
Dependency<System.Collections.Generic.IDictionary<int, double>, System.Collections.Generic.IList<string>>(Sample.Dependency2 dep<--Sample.Dependency2))
  +[Dependency<System.Collections.Generic.IDictionary<int, double>, System.Collections.Generic.IList<string>>(Sample.Dependency2 dep<--Sample.Dependency2))]<--[Sample.Dependency2]--[Dependency2()]
Dependency2()
""");
    }
    
    [Fact]
    public async Task ShouldSupportAutoBinding()
    {
        // Given

        // When
        var result = await """
namespace Sample
{
    using System;
    using Pure.DI;
    using Sample;

    internal interface IService
    {
    }

    internal class Service : IService
    {
        internal Service(Dependency dependency)
        {
        }
    }

    internal class Dependency
    {
        public Dependency() {}
    }

    internal partial class Composer
    {                   
        private static void Setup() => Pure.DI.DI.Setup("Composer")
            .Bind<IService>().To<Service>(); 
    }

    public class Program { public static void Main() { } }               
}
""".RunAsync();

        // Then
        result.Success.ShouldBeTrue(result.GeneratedCode);
        var graphs = GetGraphs(result);
        graphs.Length.ShouldBe(1, result.GeneratedCode);
        graphs[0].ConvertToString().ShouldBe("""
Service(Sample.Dependency dependency<--Sample.Dependency))
  +[Service(Sample.Dependency dependency<--Sample.Dependency))]<--[Sample.Dependency]--[Dependency()]
Dependency()
""");
    }
    
    [Fact]
    public async Task ShouldSupportComplexAutoBinding()
    {
        // Given

        // When
        var result = await """
namespace Sample
{
    using System;
    using Pure.DI;
    using Sample;

    internal interface IService
    {
    }

    internal class Service : IService
    {
        internal Service(Dependency dependency)
        {
        }
    }

    internal interface IDependency2
    {
    }

    internal class Dependency2: IDependency2
    {
        public Dependency2() {}
    }

    internal class Dependency
    {
        public Dependency(IDependency2 dep) {}
    }

    internal partial class Composer
    {                   
        private static void Setup() => Pure.DI.DI.Setup("Composer")
            .Bind<IDependency2>().To<Dependency2>()
            .Bind<IService>().To<Service>(); 
    }

    public class Program { public static void Main() { } }               
}
""".RunAsync();

        // Then
        result.Success.ShouldBeTrue(result.GeneratedCode);
        var graphs = GetGraphs(result);
        graphs.Length.ShouldBe(1, result.GeneratedCode);
        graphs[0].ConvertToString().ShouldBe("""
Dependency2()
Service(Sample.Dependency dependency<--Sample.Dependency))
  +[Service(Sample.Dependency dependency<--Sample.Dependency))]<--[Sample.Dependency]--[Dependency(Sample.IDependency2 dep<--Sample.IDependency2))]
Dependency(Sample.IDependency2 dep<--Sample.IDependency2))
  +[Dependency(Sample.IDependency2 dep<--Sample.IDependency2))]<--[Sample.IDependency2]--[Dependency2()]
""");
    }
    
    [Fact]
    public async Task ShouldSupportAutoBindingWhenGeneric()
    {
        // Given

        // When
        var result = await """
namespace Sample
{
    using System;
    using Pure.DI;
    using Sample;

    internal interface IService
    {
    }

    internal class Service : IService
    {
        internal Service(Dependency<string> dependency)
        {
        }
    }

    internal class Dependency<T>
    {
        public Dependency() {}
    }

    internal partial class Composer
    {                   
        private static void Setup() => Pure.DI.DI.Setup("Composer")
            .Bind<IService>().To<Service>(); 
    }

    public class Program { public static void Main() { } }               
}
""".RunAsync();

        // Then
        result.Success.ShouldBeTrue(result.GeneratedCode);
        var graphs = GetGraphs(result);
        graphs.Length.ShouldBe(1, result.GeneratedCode);
        graphs[0].ConvertToString().ShouldBe("""
Service(Sample.Dependency<string> dependency<--Sample.Dependency<string>))
  +[Service(Sample.Dependency<string> dependency<--Sample.Dependency<string>))]<--[Sample.Dependency<string>]--[Dependency<string>()]
Dependency<string>()
""");
    }
    
    [Fact]
    public async Task ShouldHandleCyclicDependencies()
    {
        // Given

        // When
        var result = await """
namespace Sample
{
    using System;
    using Pure.DI;
    using Sample;

    internal interface IService
    {
    }

    internal interface IDependency1
    {
    }

    internal interface IDependency2
    {
    }

    internal class Service : IService
    {
        internal Service(IDependency1 dep) {}
    }

    internal class Dependency1 : IDependency1
    {
        public Dependency1(IDependency2 dep) {}
    }

    internal class Dependency2 : IDependency2
    {
        public Dependency2(IService service) {}
    }

    internal partial class Composer
    {                   
        private static void Setup() => Pure.DI.DI.Setup("Composer")
            .Bind<IDependency1>().To<Dependency1>()
            .Bind<IDependency2>().To<Dependency2>()
            .Bind<IService>().To<Service>()
            .Root<IService>("Service"); 
    }

    public class Program { public static void Main() { } }               
}
""".RunAsync();

        // Then
        result.Success.ShouldBeFalse(result.GeneratedCode);
        var graphs = GetGraphs(result);
        graphs.Length.ShouldBe(1, result.GeneratedCode);
        graphs[0].ConvertToString().ShouldBe("""
Sample.IService() Service
  +[Sample.IService() Service]<--[Sample.IService]--[Service(Sample.IDependency1 dep<--Sample.IDependency1))]
Dependency1(Sample.IDependency2 dep<--Sample.IDependency2))
  +[Dependency1(Sample.IDependency2 dep<--Sample.IDependency2))]<--[Sample.IDependency2]--[Dependency2(Sample.IService service<--Sample.IService))]
Dependency2(Sample.IService service<--Sample.IService))
  +[Dependency2(Sample.IService service<--Sample.IService))]<--[Sample.IService]--[Service(Sample.IDependency1 dep<--Sample.IDependency1))]
Service(Sample.IDependency1 dep<--Sample.IDependency1))
  +[Service(Sample.IDependency1 dep<--Sample.IDependency1))]<--[Sample.IDependency1]--[Dependency1(Sample.IDependency2 dep<--Sample.IDependency2))]
""");
        
        var errors = result.Logs.Where(i => i.Id == LogId.ErrorCyclicDependency).ToImmutableArray();
        errors.Length.ShouldBe(1);
    }
    
    [Fact]
    public async Task ShouldSupportSeveralArgs()
    {
        // Given

        // When
        var result = await """
using System;
using Pure.DI;

namespace Sample
{
    interface IDependency {}

    class Dependency: IDependency
    {
        int _id;

        public Dependency(int id)
        { 
            _id = id;            
        }

        public override string ToString() => _id.ToString();
    }

    interface IService
    {
        IDependency Dep { get; }

        string Name { get; }
    }

    class Service: IService 
    {
        int _id;
        string _name;

        public Service(IDependency dep, [Tag(99)] int id, string name)
        { 
            Dep = dep;
            _id = id;
            _name = name;
        }

        public IDependency Dep { get; }

        public string Name => $"{_name} {_id} {Dep}";
    }

    static class Setup
    {
        private static void SetupComposer()
        {
            DI.Setup("Composer")
                .Bind<IDependency>().As(Lifetime.Singleton).To<Dependency>()
                .Bind<IService>().To<Service>()    
                .Arg<string>("serviceName")           
                .Arg<int>("id", 99)
                .Arg<int>("depId")
                .Root<IService>("Service");
        }
    }

    public class Program
    {
        public static void Main()
        {
            var composer = new Composer("Some Name", 37, 56);
            Console.WriteLine(composer.Service.Name);                               
        }
    }                
}
""".RunAsync();

        // Then
        result.Success.ShouldBeTrue(result.GeneratedCode);
        var graphs = GetGraphs(result);
        graphs.Length.ShouldBe(1, result.GeneratedCode);
        graphs[0].ConvertToString().ShouldBe("""
Sample.IService() Service
  +[Sample.IService() Service]<--[Sample.IService]--[Service(Sample.IDependency dep<--Sample.IDependency), int id<--int(99)), string name<--string))]
Dependency(int id<--int))
  +[Dependency(int id<--int))]<--[int]--[int depId]
Service(Sample.IDependency dep<--Sample.IDependency), int id<--int(99)), string name<--string))
  +[Service(Sample.IDependency dep<--Sample.IDependency), int id<--int(99)), string name<--string))]<--[int(99)]--[int id]
  +[Service(Sample.IDependency dep<--Sample.IDependency), int id<--int(99)), string name<--string))]<--[Sample.IDependency]--[Dependency(int id<--int))]
  +[Service(Sample.IDependency dep<--Sample.IDependency), int id<--int(99)), string name<--string))]<--[string]--[string serviceName]
string serviceName
int id
int depId
""");
    }
    
    [Fact]
    public async Task ShouldSupportSeveralGraphs()
    {
        // Given

        // When
        var result = await """
namespace Sample
{
    using System;
    using Pure.DI;
    using Sample;

    internal interface IService1 { }

    internal interface IDependency1 { }

    internal class Service1 : IService1
    {
        internal Service1(IDependency1 dependency) {}
    }

    internal class Dependency1 : IDependency1
    {
        public Dependency1() {}
    }

    internal interface IService2 { }

    internal interface IDependency2 { }

    internal class Service2 : IService2
    {
        internal Service2(IDependency2 dependency) {}
    }

    internal class Dependency2 : IDependency2
    {
        public Dependency2() {}
    }

    internal partial class Composer
    {                   
        private static void Setup1() => Pure.DI.DI.Setup("Composer1")
            .Bind<IDependency1>().To<Dependency1>()
            .Bind<IService1>().To<Service1>();        

        private static void Setup2() => Pure.DI.DI.Setup("Composer2")
            .Bind<IDependency2>().To<Dependency2>()
            .Bind<IService2>().To<Service2>();
    }               

    public class Program { public static void Main() { } }
}
""".RunAsync();

        // Then
        result.Success.ShouldBeTrue(result.GeneratedCode);
        var graphs = GetGraphs(result);
        graphs.Length.ShouldBe(2, result.GeneratedCode);
        graphs[0].ConvertToString().ShouldBe("""
Dependency1()
Service1(Sample.IDependency1 dependency<--Sample.IDependency1))
  +[Service1(Sample.IDependency1 dependency<--Sample.IDependency1))]<--[Sample.IDependency1]--[Dependency1()]
""");
        graphs[1].ConvertToString().ShouldBe("""
Dependency2()
Service2(Sample.IDependency2 dependency<--Sample.IDependency2))
  +[Service2(Sample.IDependency2 dependency<--Sample.IDependency2))]<--[Sample.IDependency2]--[Dependency2()]
""");
    }

    private static DependencyGraph[] GetGraphs(Result result) => 
        result.DependencyGraphs.ToArray();
}