// Run this from the working directory where the solution or project to build is located.

using System.CommandLine;
using Build;
using Pure.DI;
#pragma warning disable CS0162

var readmeCommand = new Command("readme", "Generates README.MD");
readmeCommand.SetHandler(ctx => Composition.Resolve<ITarget>("readme").RunAsync(ctx));
readmeCommand.AddAlias("r");

return await new RootCommand
{
    readmeCommand
}.InvokeAsync(args);

DI.Setup("Composition")
    .Bind<ITarget>("readme").To<ReadmeTarget>();