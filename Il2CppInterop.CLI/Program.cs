using System.CommandLine;
using Il2CppInterop.CLI.Commands;
using Serilog;
using Serilog.Core;

var loggingLevelSwitch = new LoggingLevelSwitch();
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Il2CppInterop", loggingLevelSwitch)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

try
{
    var cliConfiguration = new CliConfiguration(new Il2CppInteropRootCommand(loggingLevelSwitch));
    return await cliConfiguration.InvokeAsync(args);
}
finally
{
    Log.CloseAndFlush();
}
