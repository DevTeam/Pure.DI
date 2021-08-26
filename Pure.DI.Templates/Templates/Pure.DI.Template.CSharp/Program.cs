namespace _PureDIProjectName_
{
    public class Program
    {
        public static int Main() => $(ComposerName).Resolve<Program>().Run();

        private readonly IInput _input;
        private readonly IOutput _output;

        internal Program(IInput input, IOutput output)
        {
            _input = input;
            _output = output;
        }

        private int Run()
        {
            _output.WriteLine("Hello!");

            _output.WriteLine("Press enter to exit.");
            _input.ReadLine();
            return 0;
        }
    }
}