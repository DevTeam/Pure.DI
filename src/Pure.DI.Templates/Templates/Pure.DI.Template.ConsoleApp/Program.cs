// See https://github.com/DevTeam/Pure.DI for more information
var composition = new $(CompositionName)();
return composition.Root.Run(args);

/// <summary>
/// The application entry point and composition root. Its dependencies are supplied by the generated
/// composition, so <see cref="IInput"/> and <see cref="IOutput"/> can be replaced without changing this class.
/// </summary>
/// <param name="input">The source of user input.</param>
/// <param name="output">The sink for program output.</param>
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