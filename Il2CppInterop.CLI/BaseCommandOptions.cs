using Microsoft.Extensions.Logging;

namespace Il2CppInterop.CLI;

internal abstract record BaseCommandOptions(bool Verbose)
{
    public ILogger CreateLogger()
    {
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder
                .AddFilter("Il2CppInterop", Verbose ? LogLevel.Trace : LogLevel.Information)
                .AddSimpleConsole(opt =>
                {
                    opt.SingleLine = true;
                });
        });

        return loggerFactory.CreateLogger("Il2CppInterop");
    }
}
