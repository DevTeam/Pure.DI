// The application entry point and the composition root.
// This is the only place in the application where the object graph is created.
return $(ComposerName).Resolve<Program>().Run(args);

internal partial class Program
{
    /// <summary>
    /// The abstract input.
    /// </summary>
    private readonly IInput _input;
    
    /// <summary>
    /// The abstract output.
    /// </summary>
    private readonly IOutput _output;

    /// <summary>
    /// The constructor creates an instance, verifies, and stores dependencies into private fields.
    /// </summary>
    /// <param name="input">The abstract input.</param>
    /// <param name="output">The abstract output.</param>
    internal Program(IInput input, IOutput output)
    {
        _input = input ?? throw new ArgumentNullException(nameof(input));
        _output = output ?? throw new ArgumentNullException(nameof(output));
    }

    // ReSharper disable once UnusedParameter.Local
    /// <summary>
    /// Runs the application logic.
    /// </summary>
    /// <returns>The application exit code.</returns>
    private int Run(string[] args)
    {
        _output.WriteLine("Hello!");

        _output.WriteLine("Press enter to exit.");
        _input.ReadLine();

        return 0;
    }
}