﻿namespace Pure.DI.IntegrationTests;

public class ShroedingersCatTests
{
    [Theory]
    [InlineData(NullableContextOptions.Disable)]
    [InlineData(NullableContextOptions.Annotations)]
    [InlineData(NullableContextOptions.Warnings)]
    [InlineData(NullableContextOptions.Enable)]
    public async Task ShroedingersCatScenario(NullableContextOptions nullableContextOptions)
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;
                           using static Pure.DI.Lifetime;

                           namespace Sample
                           {
                               // Let's create an abstraction
                           
                               interface IBox<out T> { T Content { get; } }
                           
                               enum State { Alive, Dead }
                           
                               interface ICat { State State { get; } }
                           
                               // Here is our implementation
                           
                               class CardboardBox<T> : IBox<T>
                               {
                                   public CardboardBox(T content, Random rnd) => Content = content;
                           
                                   public T Content { get; }
                           
                                   public override string ToString() => $"[{Content}]";
                               }
                           
                               class ShroedingersCat : ICat
                               {
                                   // Represents the superposition of the states
                                   private readonly Lazy<State> _superposition;
                           
                                   public ShroedingersCat(Lazy<State> superposition, Random rnd) => _superposition = superposition;
                           
                                   // The decoherence of the superposition at the time of observation via an irreversible process
                                   public State State => _superposition.Value;
                           
                                   public override string ToString() => $"{State} cat";
                               }
                           
                               // Let's glue all together
                           
                               internal partial class Composition
                               {
                                   void Setup()
                                   {
                                       // FormatCode = On
                                       DI.Setup(nameof(Composition))
                                           // Models a random subatomic event that may or may not occur
                                           .Bind<Random>().As(Singleton).To(_ => 
                                           {
                                               var i = 0;
                                               i++;
                                               return new Random();
                                           })
                                           // Represents a quantum superposition of 2 states: Alive or Dead
                                           .Bind<State>().To(ctx =>
                                           {
                                               ctx.Inject<Random>(out var random);
                                               return (State)random.Next(2);
                                           }) 
                                           .Bind<ICat>().To<ShroedingersCat>()
                                           // Represents a cardboard box with any content
                                           .Bind<IBox<TT>>().To<CardboardBox<TT>>() 
                                           // Composition Root
                                           .Root<Program>("Root");
                                   }
                               }
                           
                               public class Program
                               {
                                   IBox<ICat> _box;
                           
                                   internal Program(IBox<ICat> box) => _box = box;
                           
                                   private void Run() => Console.WriteLine(_box);
                           
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       composition.Root.Run();
                                       Console.WriteLine(composition);
                                   }
                               }
                           }
                           """.RunAsync(new Options
        {
            LanguageVersion = LanguageVersion.CSharp9,
            NullableContextOptions = nullableContextOptions
        });

        // Then
        result.Success.ShouldBeTrue(result);
        (result.StdOut.Contains("[Dead cat]") || result.StdOut.Contains("[Alive cat]")).ShouldBeTrue(result);
        var lines = result.GeneratedCode.Split(Environment.NewLine);
        lines.Count(i => i.Contains(" = new Random();")).ShouldBe(1, result);
        lines.Count(i => i.Contains("EnsureExistenceOf_")).ShouldBe(3, result);
    }
}