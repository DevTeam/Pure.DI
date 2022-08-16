namespace Pure.DI.Tests.Integration;

public class FuncResolveTests
{
    [Fact]
    public void ShouldSupportArray()
    {
        // Given

        // When
        var output = @"
            namespace Sample
            {
                using System;
                using Pure.DI;

                public class CompositionRoot
                {
                    public readonly int Value;
                    internal CompositionRoot([Tag(1)] IDep<int>[] value) => Value = value.Length;
                }

                public interface IDep<T> { }

                public class Dep<T>: IDep<T> { }

                internal static partial class Composer
                {
                    static Composer()
                    {
                        DI.Setup()
                            .Bind<IDep<int>>(2).To<Dep<int>>()
                            .Bind<IDep<int>[]>(1).To(ctx => new IDep<int>[]{ctx.Resolve<IDep<int>>(2), ctx.Resolve<IDep<int>>(2)})
                            .Bind<CompositionRoot>().To<CompositionRoot>();
                    }
                }
            }".Run(out var generatedCode);

        // Then
        output.ShouldBe(new[]
        {
            "2"
        }, generatedCode);
    }

    [Fact]
    public void ShouldSupportGenericArray()
    {
        // Given

        // When
        var output = @"
            namespace Sample
            {
                using System;
                using Pure.DI;

                public class CompositionRoot
                {
                    public readonly int Value;
                    internal CompositionRoot([Tag(1)] IDep<int>[] value) => Value = value.Length;
                }

                public interface IDep<T> { }

                public class Dep<T>: IDep<T> { }

                internal static partial class Composer
                {
                    static Composer()
                    {
                        DI.Setup()
                            .Bind<IDep<TT>>(2).To<Dep<TT>>()
                            .Bind<IDep<TT>[]>(1).To(ctx => new IDep<TT>[]{ctx.Resolve<IDep<TT>>(2), ctx.Resolve<IDep<TT>>(2)})
                            .Bind<CompositionRoot>().To<CompositionRoot>();
                    }
                }
            }".Run(out var generatedCode);

        // Then
        output.ShouldBe(new[]
        {
            "2"
        }, generatedCode);
    }

    [Fact]
    public void ShouldSupportGenericCast()
    {
        // Given

        // When
        var output = @"
            namespace Sample
            {
                using System;
                using Pure.DI;

                public class CompositionRoot
                {
                    public readonly int Value;
                    internal CompositionRoot([Tag(1)] IDep<int>[] value) => Value = value.Length;
                }

                public interface IDep<T> { }

                public class Dep<T>: IDep<T> { }

                internal static partial class Composer
                {
                    static Composer()
                    {
                        DI.Setup()
                            .Bind<IDep<TT>>().To<Dep<TT>>()
                            .Bind<IDep<TT>[]>(1).To(ctx => new IDep<TT>[]{(IDep<TT>)ctx.Resolve<IDep<TT>>()})
                            .Bind<CompositionRoot>().To<CompositionRoot>();
                    }
                }
            }".Run(out var generatedCode);

        // Then
        output.ShouldBe(new[]
        {
            "1"
        }, generatedCode);
    }

    [Fact]
    public void ShouldSupportGenericVar()
    {
        // Given

        // When
        var output = @"
            namespace Sample
            {
                using System;
                using Pure.DI;

                public class CompositionRoot
                {
                    public readonly int Value;
                    internal CompositionRoot([Tag(1)] IDep<int>[] value) => Value = value.Length;
                }

                public interface IDep<T> { }

                public class Dep<T>: IDep<T> { }

                internal static partial class Composer
                {
                    static Composer()
                    {
                        DI.Setup()
                            .Bind<IDep<TT>>().To<Dep<TT>>()
                            .Bind<IDep<TT>[]>(1).To(ctx => {
                                IDep<TT> val = (IDep<TT>)ctx.Resolve<IDep<TT>>();
                                return new IDep<TT>[] {val};
                            })
                            .Bind<CompositionRoot>().To<CompositionRoot>();
                    }
                }
            }".Run(out var generatedCode);

        // Then
        output.ShouldBe(new[]
        {
            "1"
        }, generatedCode);
    }

    [Fact]
    public void ShouldSupportGenericSimpleLambda()
    {
        // Given

        // When
        var output = @"
            namespace Sample
            {
                using System;
                using Pure.DI;

                public class CompositionRoot
                {
                    public readonly int Value;
                    internal CompositionRoot([Tag(1)] IDep<int>[] value) => Value = value.Length;
                }

                public interface IDep<T> { }

                public class Dep<T>: IDep<T> { }

                internal static partial class Composer
                {
                    static Composer()
                    {
                        DI.Setup()
                            .Bind<IDep<TT>>().To<Dep<TT>>()
                            .Bind<IDep<TT>[]>(1).To(ctx => {
                                var fn = new Func<IDep<TT>>(() => (IDep<TT>)ctx.Resolve<IDep<TT>>());
                                return new IDep<TT>[] {fn(), ctx.Resolve<IDep<TT>>()};
                            })
                            .Bind<CompositionRoot>().To<CompositionRoot>();
                    }
                }
            }".Run(out var generatedCode);

        // Then
        output.ShouldBe(new[]
        {
            "2"
        }, generatedCode);
    }

    [Fact]
    public void ShouldSupportGenericLambda()
    {
        // Given

        // When
        var output = @"
            namespace Sample
            {
                using System;
                using Pure.DI;

                public class CompositionRoot
                {
                    public readonly int Value;
                    internal CompositionRoot([Tag(1)] IDep<int>[] value) => Value = value.Length;
                }

                public interface IDep<T> { }

                public class Dep<T>: IDep<T> { }

                internal static partial class Composer
                {
                    static Composer()
                    {
                        DI.Setup()
                            .Bind<IDep<TT>>().To<Dep<TT>>()
                            .Bind<IDep<TT>[]>(1).To(ctx => {
                                var fn = new Func<IDep<TT>>(() => { return (IDep<TT>)ctx.Resolve<IDep<TT>>();});
                                return new IDep<TT>[] {fn()};
                            })
                            .Bind<CompositionRoot>().To<CompositionRoot>();
                    }
                }
            }".Run(out var generatedCode);

        // Then
        output.ShouldBe(new[]
        {
            "1"
        }, generatedCode);
    }

    [Fact]
    public void ShouldSupportGenericTypeOf()
    {
        // Given

        // When
        var output = @"
            namespace Sample
            {
                using System;
                using Pure.DI;

                public class CompositionRoot
                {
                    public readonly int Value;
                    internal CompositionRoot([Tag(1)] IDep<int>[] value) => Value = value.Length;
                }

                public interface IDep<T> { }

                public class Dep<T>: IDep<T> { }

                internal static partial class Composer
                {
                    static Composer()
                    {
                        DI.Setup()
                            .Bind<IDep<TT>>().To<Dep<TT>>()
                            .Bind<IDep<TT>[]>(1).To(ctx => {
                                System.Console.WriteLine(typeof(TT).Name);
                                return new IDep<TT>[] {ctx.Resolve<IDep<TT>>()};
                            })
                            .Bind<CompositionRoot>().To<CompositionRoot>();
                    }
                }
            }".Run(out var generatedCode);

        // Then
        output.ShouldBe(new[]
        {
            nameof(Int32), "1"
        }, generatedCode);
    }

    [Fact]
    public void ShouldSupportGenericLocalFunction()
    {
        // Given

        // When
        var output = @"
            namespace Sample
            {
                using System;
                using Pure.DI;

                public class CompositionRoot
                {
                    public readonly int Value;
                    internal CompositionRoot([Tag(1)] IDep<int>[] value) => Value = value.Length;
                }

                public interface IDep<T> { }

                public class Dep<T>: IDep<T> { }

                internal static partial class Composer
                {
                    static Composer()
                    {
                        DI.Setup()
                            .Bind<IDep<TT>>().To<Dep<TT>>()
                            .Bind<IDep<TT>[]>(1).To(ctx => {
                                IDep<TT> Get() { return (IDep<TT>)ctx.Resolve<IDep<TT>>(); };
                                return new IDep<TT>[] {Get(), Get()};
                            })
                            .Bind<CompositionRoot>().To<CompositionRoot>();
                    }
                }
            }".Run(out var generatedCode);

        // Then
        output.ShouldBe(new[]
        {
            "2"
        }, generatedCode);
    }

    [Fact]
    public void ShouldSupportGenericPatternMatching()
    {
        // Given

        // When
        // ReSharper disable once RedundantStringInterpolation
        var output = $@"
            namespace Sample
            {{
                using System;
                using Pure.DI;

                public class CompositionRoot
                {{
                    public readonly int Value;
                    internal CompositionRoot([Tag(1)] IDep<int>[] value) => Value = value.Length;
                }}

                public interface IDep<T> {{ }}

                public class Dep<T>: IDep<T> {{ }}

                internal static partial class Composer
                {{
                    static Composer()
                    {{
                        DI.Setup()
                            .Bind<IDep<TT>>().To<Dep<TT>>()
                            .Bind<IDep<TT>[]>(1).To(ctx => {{
                                switch(ctx.Resolve<IDep<TT>>())
                                {{
                                    case IDep<TT> dep:
                                        return new IDep<TT>[]{{dep, dep, dep}};
                                    default: 
                                        return default;
                                }}
                            }})
                            .Bind<CompositionRoot>().To<CompositionRoot>();
                    }}
                }}
            }}".Run(out var generatedCode);

        // Then
        output.ShouldBe(new[]
        {
            "3"
        }, generatedCode);
    }

    [Fact]
    public void ShouldSupportComplexFunc()
    {
        // Given

        // When
        var output = @"
            namespace Sample
            {
                using System;
                using System.Linq;
                using Pure.DI;

                public class CompositionRoot
                {
                    public readonly int Value;
                    internal CompositionRoot([Tag(1)] Func<int, IDep<int>[]> value) => Value = value(3).Length;
                }

                public interface IDep<T> { }

                public class Dep<T>: IDep<T> { }

                internal static partial class Composer
                {
                    static Composer()
                    {
                        DI.Setup()
                            .Bind<IDep<TT>>().To<Dep<TT>>()
                            .Bind<Func<int, IDep<TT>[]>>(1).To(ctx => new Func<int, IDep<TT>[]>(size => Enumerable.Repeat(ctx.Resolve<IDep<TT>>(), size).ToArray()))
                            .Bind<CompositionRoot>().To<CompositionRoot>();
                    }
                }
            }".Run(out var generatedCode);

        // Then
        output.ShouldBe(new[]
        {
            "3"
        }, generatedCode);
    }
    
    [Fact]
    public void ShouldSupportComplexFuncWithResolve()
    {
        // Given

        // When
        var output = @"
            namespace Sample
            {
                using System;
                using System.Linq;
                using Pure.DI;

                public class CompositionRoot
                {
                    public readonly string Value;

                    internal CompositionRoot([Tag(1)] Func<string, IDep> value) => Value = value(""Abc"").Name;
                }

                public interface IDep { string Name { get; } }

                public class Dep: IDep { public string Name { get; set; } }

                internal static partial class Composer
                {
                    static Composer()
                    {
                        DI.Setup()
                            .Bind<IDep>().Bind<Dep>().To<Dep>()
                            .Bind<Func<string, IDep>>(1).To(ctx => new Func<string, IDep>(name =>
                            {
                                var dep = ctx.Resolve<Dep>();
                                dep.Name = name;
                                return dep;
                            }))
                            .Bind<CompositionRoot>().To<CompositionRoot>();
                    }
                }
            }".Run(out var generatedCode);

        // Then
        output.ShouldBe(new[]
        {
            "Abc"
        }, generatedCode);
    }
    
    [Fact]
    public void ShouldSupportResolveFunctionName()
    {
        // Given

        // When
        var output = @"
            namespace Sample
            {
                using System;
                using Pure.DI;

                public class CompositionRoot
                {
                    public readonly int Value;
                    internal CompositionRoot([Tag(1)] int[] value) => Value = value[0];
                }

                public interface IDep<T> { }

                public class Dep<T>: IDep<T> { }

                internal static partial class Composer
                {
                    static Composer()
                    {
                        DI.Setup()
                            .Bind<int[]>(1).To(_ => Utils.Resolve<int[]>())
                            .Bind<CompositionRoot>().To<CompositionRoot>();
                    }                  
                }

                public class Utils
                {
                    public static T Resolve<T>()
                    { 
                        if (typeof(T) == typeof(int[])) return (T)(object)new int[] {1, 2};
                        return default(T);
                    }  
                }               
            }".Run(out var generatedCode);

        // Then
        output.ShouldBe(new[]
        {
            "1"
        }, generatedCode);
    }
    
    [Fact]
    // ReSharper disable once InconsistentNaming
    public void ShouldSupportResolveTTFunctionWhenGenericTT()
    {
        // Given

        // When
        var output = @"
            namespace Sample
            {
                using System;
                using Pure.DI;

                public class CompositionRoot
                {
                    public readonly int Value;
                    internal CompositionRoot([Tag(1)] int value) => Value = value;
                }

                public interface IDep<T> { }

                public class Dep<T>: IDep<T> { }

                internal static partial class Composer
                {
                    static Composer()
                    {
                        DI.Setup()
                            .Bind<TT>(1).To(_ => Utils.Resolve<TT>())
                            .Bind<CompositionRoot>().To<CompositionRoot>();
                    }                  
                }

                public class Utils
                {
                    public static T Resolve<T>()
                    { 
                        if (typeof(T) == typeof(int)) return (T)(object)1;
                        return default(T);
                    }  
                }               
            }".Run(out var generatedCode);

        // Then
        output.ShouldBe(new[]
        {
            "1"
        }, generatedCode);
    }
    
    [Fact]
    // ReSharper disable once InconsistentNaming
    public void ShouldSupportResolveTTFunctionWhenGenericAndOtherNamespace()
    {
        // Given

        // When
        var output = @"
            namespace Sample
            {
                using System;
                using Pure.DI;

                public class CompositionRoot
                {
                    public readonly int Value;
                    internal CompositionRoot([Tag(1)] int value) => Value = value;
                }

                public interface IDep<T> { }

                public class Dep<T>: IDep<T> { }

                internal static partial class Composer
                {
                    static Composer()
                    {
                        DI.Setup()
                            .Bind<TT>(1).To(_ => Other.Utils.Resolve<TT>())
                            .Bind<CompositionRoot>().To<CompositionRoot>();
                    }                  
                }                             
            }
            
            namespace Other
            {
                public class Utils
                {
                    public static T Resolve<T>()
                    { 
                        if (typeof(T) == typeof(int)) return (T)(object)1;
                        return default(T);
                    }  
                }
            }
".Run(out var generatedCode);

        // Then
        output.ShouldBe(new[]
        {
            "1"
        }, generatedCode);
    }
    
    [Fact]
    // ReSharper disable once InconsistentNaming
    public void ShouldSupportResolveFunctionTTWhenFunc()
    {
        // Given

        // When
        var output = @"
            namespace Sample
            {
                using System;
                using Pure.DI;

                public class CompositionRoot
                {
                    public readonly int Value;
                    internal CompositionRoot([Tag(1)] Func<int> value) => Value = value();
                }

                public interface IDep<T> { }

                public class Dep<T>: IDep<T> { }

                internal static partial class Composer
                {
                    static Composer()
                    {
                        DI.Setup()
                            .Bind<TT>(1).To(_ => Other.Utils.Resolve<TT>())
                            .Bind<CompositionRoot>().To<CompositionRoot>();
                    }                  
                }                             
            }
            
            namespace Other
            {
                public class Utils
                {
                    public static T Resolve<T>()
                    { 
                        if (typeof(T) == typeof(int)) return (T)(object)1;
                        return default(T);
                    }  
                }
            }
".Run(out var generatedCode);

        // Then
        output.ShouldBe(new[]
        {
            "1"
        }, generatedCode);
    }
}