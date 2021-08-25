namespace _PureDIProjectName_
{
    public class Program
    {
        public static int Main() => $(ComposerName).Resolve<Program>().Run();

        private readonly IStdIn _stdIn;
        private readonly IStdOut _stdOut;

        internal Program(IStdIn stdIn, IStdOut stdOut)
        {
            _stdIn = stdIn;
            _stdOut = stdOut;
        }

        private int Run()
        {
            _stdOut.WriteLine("Hello!");

            _stdOut.WriteLine("Press any key to exit.");
            _stdIn.ReadLine();
            return 0;
        }
    }
}