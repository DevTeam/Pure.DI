// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable CA1822
namespace Pure.DI.UsageScenarios.Tests
{
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.TestHost;
    using Microsoft.Extensions.DependencyInjection;
    using Shouldly;
    using Xunit;
    
    // $visible=true
    // $tag=5 Advanced
    // $priority=04
    // $description=ASPNET
    // {
    public class AspNetMvc
    {
    // }
        [Fact]
    // {
        public async Task Run()
        {
            var hostBuilder = new WebHostBuilder().UseStartup<Startup>();
            using var server = new TestServer(hostBuilder);
            using var client = server.CreateClient();
            
            var response = await client.GetStringAsync("/Greeting");
            response.ShouldBe("Hello!");
        }
    }

    public interface IGreeting
    {
        string Hello { get; }
    }

    public class Greeting : IGreeting
    {
        public string Hello => "Hello!";
    }

    [ApiController]
    [Route("[controller]")]
    public class GreetingController : ControllerBase
    {
        private readonly IGreeting _greeting;

        public GreetingController(IGreeting greeting) => _greeting = greeting;

        [HttpGet] public string Get() => _greeting.Hello;
    }
    
    public static partial class GreetingDomain
    {
        static GreetingDomain()
        {
            DI.Setup()
                .Bind<IGreeting>().As(Lifetime.ContainerSingleton).To<Greeting>()
                .Bind<GreetingController>().To<GreetingController>();
        }
    }

    public class Startup
    {
        public Startup(Microsoft.Extensions.Configuration.IConfiguration configuration) =>
            Configuration = configuration;

        public Microsoft.Extensions.Configuration.IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.AddGreetingDomain();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
    // }
}
