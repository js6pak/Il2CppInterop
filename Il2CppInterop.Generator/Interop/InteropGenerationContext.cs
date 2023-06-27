using System.Diagnostics.CodeAnalysis;
using AsmResolver.DotNet;

namespace Il2CppInterop.Generator.Interop;

public sealed class InteropGenerationContext
{
    private readonly SimpleAssemblyResolver _assemblyResolver = new();
    private readonly Dictionary<AssemblyDefinition, InteropAssemblyContext> _assemblyMap = new();
    private readonly List<InteropTypeContext> _topLevelTypes = new();

    public InteropGenerationContext(InputContext input)
    {
        Input = input;
    }

    public InputContext Input { get; }

    public IReadOnlyCollection<InteropAssemblyContext> Assemblies => _assemblyMap.Values;
    public IReadOnlyList<InteropTypeContext> TopLevelTypes => _topLevelTypes;

    public InteropCorLibContext CorLibContext { get; private set; } = null!;

    public void CreateAssembliesAndEmptyTypeDefinitions()
    {
        foreach (var originalAssembly in Input.Assemblies)
        {
            var interopAssembly = originalAssembly.IsCorLib
                ? CorLibContext = new InteropCorLibContext(this, originalAssembly, _assemblyResolver)
                : new InteropAssemblyContext(this, originalAssembly, _assemblyResolver);

            _assemblyMap.Add(originalAssembly, interopAssembly);

            interopAssembly.CreateEmptyTypeDefinitions();
            _topLevelTypes.AddRange(interopAssembly.TopLevelTypes);
        }

        if (CorLibContext == null!)
        {
            throw new InvalidOperationException("CorLib not found");
        }
    }

    public InteropAssemblyContext GetContextForOriginal(AssemblyDefinition originalAssembly) => _assemblyMap[originalAssembly];
    public bool TryGetContextForOriginal(AssemblyDefinition originalAssembly, [MaybeNullWhen(false)] out InteropAssemblyContext context) => _assemblyMap.TryGetValue(originalAssembly, out context);
    public InteropTypeContext GetContextForOriginal(TypeDefinition originalType) => GetContextForOriginal(originalType.Module!.Assembly!).GetContextForOriginal(originalType);

    [return: NotNullIfNotNull(nameof(originalType))]
    public TypeDefinition? GetInteropTypeForOriginal(TypeDefinition? originalType) => originalType != null ? GetContextForOriginal(originalType).Definition : null;

    public void Save(string outputPath)
    {
        foreach (var interopAssembly in Assemblies)
        {
            interopAssembly.Definition.Write(Path.Combine(outputPath, interopAssembly.Definition.Name + ".dll"));
        }
    }
}
