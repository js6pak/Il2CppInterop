using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using System.Diagnostics.CodeAnalysis;
using Il2CppInterop.Generator;

namespace Il2CppInterop.CLI.Commands;

internal sealed class GenerateCommand : Command
{
    public GenerateCommand() : base("generate", "Generate wrapper assemblies that can be used to interop with Il2Cpp")
    {
        Add(new Option<DirectoryInfo>("--input", "Directory with dummy assemblies") { IsRequired = true }.ExistingOnly());
        Add(new Option<DirectoryInfo>("--output", "Directory to write generated assemblies to") { IsRequired = true });
        Add(new Option<DirectoryInfo>("--unity", "Directory with original Unity assemblies for unstripping").ExistingOnly());

        Handler = CommandHandler.Create(Handle);
    }

    public static void Handle(GenerateCommandOptions options)
    {
        var inputContext = InputContext.LoadFromDirectory(options.Input.FullName);
        if (inputContext.Assemblies.Count <= 0)
            throw new InvalidOperationException("No input assemblies found");

        InteropAssemblyGenerator.Run(new GeneratorOptions(inputContext, options.Output.FullName));
    }

    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
    public record GenerateCommandOptions(
        bool Verbose,
        DirectoryInfo Input,
        DirectoryInfo Output,
        DirectoryInfo Unity
    ) : BaseCommandOptions(Verbose);
}
