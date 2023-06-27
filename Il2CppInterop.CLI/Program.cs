using System.CommandLine;
using Il2CppInterop.CLI.Commands;

var command = new RootCommand
{
    new Option<bool>("--verbose", "Produce more verbose output")
};
command.Description = "Generate Managed<->IL2CPP interop assemblies from Cpp2IL's output.";

command.Add(new GenerateCommand());

return command.Invoke(args);
