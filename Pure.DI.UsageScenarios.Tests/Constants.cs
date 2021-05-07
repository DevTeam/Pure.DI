namespace Pure.DI.UsageScenarios.Tests
{
    using Shouldly;
    using Xunit;

    public class Constants
    {
        [Fact]
        public void Run()
        {
            // $visible=true
            // $tag=1 Basics
            // $priority=01
            // $description=Constants
            // $header=It's obvious here.
            // {
            DI.Setup()
                .Bind<int>().To(_ => 10);

            // Resolve an integer
            var val = ConstantsDI.Resolve<int>();
            // Check the value
            val.ShouldBe(10);
            // }
        }
    }
}
