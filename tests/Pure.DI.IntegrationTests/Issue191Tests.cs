namespace Pure.DI.IntegrationTests;

/// <summary>
/// A value of ctx.Let reaches only the current injection level, so a decorator passes it to the instance it decorates itself, see #191.
/// </summary>
public class Issue191Tests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ShouldPassLetArgumentToInstanceBehindDecorator(bool chain)
    {
        // Given
        var viewerBindings = chain
            ? """
              .Bind<IViewer>("base").To<Viewer>()
              .Bind<IViewer>("tracked").To(ctx =>
              {
                  ctx.Inject(out Tracker tracker);
                  ctx.Inject(out IModel model);
                  ctx.Let(model);
                  ctx.Inject<IViewer>("base", out var viewer);
                  tracker.Add(viewer);
                  return viewer;
              })
              .Bind<IViewer>().To(ctx =>
              {
                  ctx.Inject(out IModel model);
                  ctx.Let(model);
                  ctx.Inject<IViewer>("tracked", out var viewer);
                  return new LoggedViewer(viewer);
              })
              """
            : """
              .Bind<IViewer>("base").To<Viewer>()
              .Bind<IViewer>().To(ctx =>
              {
                  ctx.Inject(out Tracker tracker);
                  // The argument of the delegate
                  ctx.Inject(out IModel model);
                  ctx.Let(model);
                  ctx.Inject<IViewer>("base", out var viewer);
                  tracker.Add(viewer);
                  return viewer;
              })
              """;

        // When
        var result = await $$"""
                           using System;
                           using Pure.DI;
                           using static Pure.DI.Lifetime;

                           namespace Sample
                           {
                               public interface IModel { }

                               public sealed class Model : IModel
                               {
                                   private readonly string _name;
                                   public Model(string name) => _name = name;
                                   public override string ToString() => _name;
                               }

                               public sealed class Details
                               {
                                   private readonly IModel _model;
                                   public Details(IModel model) => _model = model;
                                   public override string ToString() => $"details of {_model}";
                               }

                               public interface IViewer { }

                               public sealed class Viewer : IViewer
                               {
                                   private readonly IModel _model;
                                   private readonly Details _details;
                                   public Viewer(IModel model, Details details) { _model = model; _details = details; }
                                   public override string ToString() => $"viewer of {_model}, {_details}";
                               }

                               public sealed class LoggedViewer : IViewer
                               {
                                   private readonly IViewer _viewer;
                                   public LoggedViewer(IViewer viewer) => _viewer = viewer;
                                   public override string ToString() => $"{_viewer}";
                               }

                               public sealed class Tracker
                               {
                                   public int Count { get; private set; }
                                   public void Add(IViewer viewer) => Count++;
                               }

                               public sealed class App
                               {
                                   private readonly Func<IModel, IViewer> _viewerFactory;
                                   public App(Func<IModel, IViewer> viewerFactory) => _viewerFactory = viewerFactory;
                                   public override string ToString() => $"{_viewerFactory(new Model("a"))}";
                               }

                               partial class Composition
                               {
                                   private static void Setup() => DI.Setup(nameof(Composition))
                                       // The argument reaches only the object the delegate creates
                                       .Bind<Func<TT1, TT>>()
                                           .As(PerBlock)
                                           .To(ctx => new Func<TT1, TT>((TT1 arg1) =>
                                           {
                                               ctx.Let<TT1>(arg1);
                                               ctx.Inject<TT>(ctx.Tag, out var value);
                                               return value;
                                           }))
                                       .Bind<IModel>().To(_ => new Model("default"))
                                       .Bind().As(Singleton).To<Tracker>()
                                       {{viewerBindings}}
                                       .Root<App>("Root");
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Root);
                                   }
                               }
                           }
                           """.RunAsync();

        // Then
        // The decorated viewer gets the argument, and its own dependencies do not.
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["viewer of a, details of default"], result);
    }
}
