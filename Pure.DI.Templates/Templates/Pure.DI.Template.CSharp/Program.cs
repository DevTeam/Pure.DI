namespace _PureDIProjectName_
{
    using System;

    public class Program
    {
        /// <summary>
        /// Entry point. Creates a composition root and runs an application logic.
        /// </summary>
        /// <returns>The application exit code.</returns>
        public static int Main() => $(ComposerName).Resolve<Program>().Run();

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

        /// <summary>
        /// Runs an application logic.
        /// </summary>
        /// <returns>The application exit code.</returns>
        private int Run()
        {
            _output.WriteLine("Hello!");

            _output.WriteLine("Press enter to exit.");
            _input.ReadLine();

            return 0;
        }
    }
}