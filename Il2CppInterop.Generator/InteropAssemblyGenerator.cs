using Il2CppInterop.Generator.Contexts.Interop;
using Il2CppInterop.Generator.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Il2CppInterop.Generator;

public sealed class InteropAssemblyGenerator
{
    public static void Run(GeneratorOptions options, ILoggerFactory? loggerFactory = null)
    {
        loggerFactory ??= NullLoggerFactory.Instance;
        var logger = loggerFactory.CreateLogger<InteropAssemblyGenerator>();

        var stopwatch = ValueStopwatch.StartNew();
        {
            var generationContext = new InteropGenerationContext(options.Input, options.InteropMethodBodyType, loggerFactory);

            // Pass 0: create all type definitions so they can be referenced later
            generationContext.CreateAssembliesAndEmptyTypeDefinitions();

            // Pass 1: setup type definitions (base type, interfaces, generics)
            generationContext.Setup();

            // Pass 2: fill type definitions (methods, fields, static constructor)
            generationContext.Fill();

            Directory.CreateDirectory(options.OutputPath);
            generationContext.Save(options.OutputPath);
        }
        logger.LogInformation("Done in {ElapsedTime}", stopwatch.GetElapsedTime().ToPrettyString());
    }
}
