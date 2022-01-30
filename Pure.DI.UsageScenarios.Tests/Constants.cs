namespace Pure.DI.UsageScenarios.Tests;

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
        // $footer=The compiler replaces the statement:
        // $footer=```CSharp
        // $footer=var val = ConstantsDI.ResolveInt();
        // $footer=```
        // $footer=by the statement:
        // $footer=```CSharp
        // $footer=var val = 10;
        // $footer=```
        // {
        DI.Setup()
            .Bind<int>().To(_ => 10);

        // Resolve an integer
        var val = ConstantsDI.ResolveInt();
        // Check the value
        val.ShouldBe(10);
        // }
    }
}