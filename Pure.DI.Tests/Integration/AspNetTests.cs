namespace Pure.DI.Tests.Integration;

public class AspNetTests
{
    [Fact]
    public void ShouldSupportAspNet()
    {
        // Given
        const string statements = @"
            //var hostBuilder = new WebHostBuilder()
            //    .ConfigureServices(x => x.AddTransient<Controller>())
            //    .UseStartup<Startup>();
            //var server = new TestServer(hostBuilder);
            //Console.WriteLine(server.CreateClient().GetStringAsync("" / controller"").Result);
            ";

        // When
        var output = @"
            namespace Sample
            {
                using Microsoft.AspNetCore.Builder;
                using Microsoft.AspNetCore.Hosting;
                using Microsoft.AspNetCore.Mvc;
                using Microsoft.AspNetCore.TestHost;
                using Microsoft.Extensions.Configuration;
                using Microsoft.Extensions.DependencyInjection;
                using System;
                using System.Threading.Tasks;
                using Pure.DI;

                public interface IMyClass<T> { }
                public class MyClass<T>: IMyClass<T> { }

                [ApiController]
                [Route(""[controller]"")]
                public class Controller : ControllerBase
                {
                    public Controller(IMyClass<string> myClass) {}

                    [HttpGet] public string Get() => ""Abc"";
                }

                public class Startup
                {
                    public Startup(Microsoft.Extensions.Configuration.IConfiguration configuration)
                    {
                        Configuration = configuration;
                    }

                    public Microsoft.Extensions.Configuration.IConfiguration Configuration { get; }

                    public void ConfigureServices(IServiceCollection services)
                    {
                        //services.AddControllers().ResolveControllersThroughServiceProvider();
                    }

                    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
                    {
                        app.UseRouting();
                        app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
                    }
                }

                static partial class Composer
                {
                    static Composer()
                    {
                        DI.Setup()
                            .Bind<IMyClass<string>>().As(Lifetime.ContainerSingleton).To<MyClass<string>>()
                            .Bind<Controller>().As(Lifetime.Scoped).To<Controller>();
                    }
                }                                    
            }".Run(out var generatedCode, new RunOptions
        {
            Statements = statements
        });

        // Then
        output.ShouldBeEmpty(generatedCode);
    }
}