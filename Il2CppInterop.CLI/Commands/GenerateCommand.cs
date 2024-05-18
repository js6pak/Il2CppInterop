using System.CommandLine;
using Il2CppInterop.Generator;

namespace Il2CppInterop.CLI.Commands;

internal sealed class GenerateCommand : CliCommand
{
    public CliOption<DirectoryInfo> Input { get; } = new CliOption<DirectoryInfo>("--input") { Description = "Directory with dummy assemblies", Required = true }.AcceptExistingOnly();
    public CliOption<DirectoryInfo> Output { get; } = new CliOption<DirectoryInfo>("--output") { Description = "Directory to write generated assemblies to", Required = true };
    public CliOption<DirectoryInfo> Unity { get; } = new CliOption<DirectoryInfo>("--unity") { Description = "Directory with dummy assemblies" }.AcceptExistingOnly();

    public CliOption<InteropMethodBodyType> InteropMethodBodyType { get; } = new("--interop-method-body-type");

    public GenerateCommand(Il2CppInteropRootCommand rootCommand) : base("generate", "Generate wrapper assemblies that can be used to interop with Il2Cpp")
    {
        Options.Add(Input);
        Options.Add(Output);
        Options.Add(Unity);

        Options.Add(InteropMethodBodyType);

        SetAction(result =>
        {
            var input = result.GetValue(Input)!;
            var output = result.GetValue(Output)!;
            var unity = result.GetValue(Unity)!;

            var inputContext = InputContext.LoadFromDirectory(input.FullName);
            if (!inputContext.Assemblies.Any())
                throw new InvalidOperationException("No input assemblies found");

            InteropAssemblyGenerator.Run(new GeneratorOptions(inputContext, output.FullName)
            {
                InteropMethodBodyType = result.GetValue(InteropMethodBodyType),
            }, rootCommand.GetLoggerFactory(result));
        });
    }
}
