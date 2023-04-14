var composition = new $(CompositionName)();
return composition.Root.Run(args);

internal partial class Program
{
    private readonly IInput _input;
    private readonly IOutput _output;

    internal Program(IInput input, IOutput output)
    {
        _input = input ?? throw new ArgumentNullException(nameof(input));
        _output = output ?? throw new ArgumentNullException(nameof(output));
    }

    private int Run(string[] args)
    {
        _output.WriteLine("Hello!");

        _output.WriteLine("Press enter to exit.");
        _input.ReadLine();

        return 0;
    }
}