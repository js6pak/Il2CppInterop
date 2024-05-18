using System.CommandLine;
using Microsoft.Extensions.Logging;
using Serilog.Core;
using Serilog.Events;
using Serilog.Extensions.Logging;

namespace Il2CppInterop.CLI.Commands;

internal sealed class Il2CppInteropRootCommand : CliRootCommand
{
    private readonly LoggingLevelSwitch _loggingLevelSwitch;

    public CliOption<bool> Verbose { get; } = new("--verbose") { Description = "Produce more verbose output" };

    public Il2CppInteropRootCommand(LoggingLevelSwitch loggingLevelSwitch) : base("Generate Managed<->IL2CPP interop assemblies from Cpp2IL's output.")
    {
        _loggingLevelSwitch = loggingLevelSwitch;

        Options.Add(Verbose);

        Subcommands.Add(new GenerateCommand(this));
    }

    public ILoggerFactory GetLoggerFactory(ParseResult parseResult)
    {
        var verbose = parseResult.GetValue(Verbose);
        _loggingLevelSwitch.MinimumLevel = verbose ? LogEventLevel.Verbose : LogEventLevel.Information;

        return new SerilogLoggerFactory();
    }
}
