using System.Diagnostics.CodeAnalysis;
using AsmResolver.DotNet;
using Il2CppInterop.Generator.Extensions;
using Il2CppInterop.Generator.MethodBody;
using Microsoft.Extensions.Logging;

namespace Il2CppInterop.Generator.Contexts.Interop;

internal sealed class InteropGenerationContext
{
    private readonly SimpleAssemblyResolver _assemblyResolver = new();
    private readonly Dictionary<AssemblyDefinition, InteropAssemblyContext> _assemblyMap = new();
    private readonly ILogger<InteropGenerationContext> _logger;

    public InteropGenerationContext(InputContext input, InteropMethodBodyType interopMethodBodyType, ILoggerFactory loggerFactory)
    {
        Input = input;
        InteropMethodBodyGenerator = interopMethodBodyType switch
        {
            InteropMethodBodyType.RuntimeInvoke => new RuntimeInvokeInteropMethodBodyGenerator(),
            InteropMethodBodyType.Direct => new DirectInteropMethodBodyGenerator(),
            _ => throw new ArgumentOutOfRangeException(nameof(interopMethodBodyType), interopMethodBodyType, null),
        };

        _logger = loggerFactory.CreateLogger<InteropGenerationContext>();
    }

    public InputContext Input { get; }

    public BaseInteropMethodBodyGenerator InteropMethodBodyGenerator { get; }

    public IReadOnlyCollection<InteropAssemblyContext> Assemblies => _assemblyMap.Values;

    public InteropCorLibContext CorLibContext { get; private set; } = null!;

    public void CreateAssembliesAndEmptyTypeDefinitions()
    {
        var stopwatch = ValueStopwatch.StartNew();
        {
            foreach (var originalAssembly in Input.Assemblies)
            {
                var interopAssembly = originalAssembly.IsCorLib
                    ? CorLibContext = new InteropCorLibContext(this, originalAssembly, _assemblyResolver)
                    : new InteropAssemblyContext(this, originalAssembly, _assemblyResolver);

                _assemblyMap.Add(originalAssembly, interopAssembly);

                interopAssembly.CreateEmptyTypeDefinitions();
            }
        }
        _logger.LogInformation("Created {Count} assembly contexts in {ElapsedTime}", Assemblies.Count, stopwatch.GetElapsedTime().ToPrettyString());

        if (CorLibContext == null!)
        {
            throw new InvalidOperationException("CorLib not found");
        }
    }

    public void Setup()
    {
        var stopwatch = ValueStopwatch.StartNew();
        {
            foreach (var assembly in Assemblies)
            {
                assembly.Setup();
            }
        }
        _logger.LogInformation("Setup finished in {ElapsedTime}", stopwatch.GetElapsedTime().ToPrettyString());
    }

    public void Fill()
    {
        var stopwatch = ValueStopwatch.StartNew();
        {
            foreach (var assembly in Assemblies)
            {
                assembly.Fill();
            }
        }
        _logger.LogInformation("Fill finished in {ElapsedTime}", stopwatch.GetElapsedTime().ToPrettyString());
    }

    public void Save(string outputPath)
    {
        var stopwatch = ValueStopwatch.StartNew();
        {
            Directory.CreateDirectory(outputPath);

            foreach (var interopAssembly in Assemblies)
            {
                interopAssembly.Definition.Write(Path.Combine(outputPath, interopAssembly.Definition.Name + ".dll"));
            }
        }
        _logger.LogInformation("Save finished in {ElapsedTime}", stopwatch.GetElapsedTime().ToPrettyString());
    }

    public InteropAssemblyContext GetContextForOriginal(AssemblyDefinition originalAssembly) => _assemblyMap[originalAssembly];
    public bool TryGetContextForOriginal(AssemblyDefinition originalAssembly, [MaybeNullWhen(false)] out InteropAssemblyContext context) => _assemblyMap.TryGetValue(originalAssembly, out context);
    public InteropTypeContext GetContextForOriginal(TypeDefinition originalType) => GetContextForOriginal(originalType.Module!.Assembly!).GetContextForOriginal(originalType);

    [return: NotNullIfNotNull(nameof(originalType))]
    public TypeDefinition? GetInteropTypeForOriginal(TypeDefinition? originalType) => originalType != null ? GetContextForOriginal(originalType).Definition : null;
}
