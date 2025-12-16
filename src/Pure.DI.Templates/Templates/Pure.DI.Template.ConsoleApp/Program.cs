// See https://github.com/DevTeam/Pure.DI for more information
var composition = new $(CompositionName)();
return composition.Root.Run(args);

internal partial class Program(IInput input, IOutput output)
{
    private int Run(string[] args)
    {
        output.WriteLine("Hello!");

        output.WriteLine("Press the Enter key to exit.");
        input.ReadLine();

        return 0;
    }
}