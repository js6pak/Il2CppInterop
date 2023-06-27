using AsmResolver.DotNet;
using AsmResolver.DotNet.Serialized;
using AsmResolver.PE;

namespace Il2CppInterop.Generator;

public class SimpleAssemblyResolver : IAssemblyResolver
{
    public Dictionary<string, AssemblyDefinition> Assemblies { get; } = new();

    public AssemblyDefinition? Resolve(AssemblyDescriptor assembly)
    {
        if (Assemblies.TryGetValue(assembly.Name!, out var ret))
            return ret;

        return null;
    }

    public void AddToCache(AssemblyDescriptor descriptor, AssemblyDefinition definition)
    {
    }

    public bool RemoveFromCache(AssemblyDescriptor descriptor)
    {
        return true;
    }

    public bool HasCached(AssemblyDescriptor descriptor)
    {
        return true;
    }

    public void ClearCache()
    {
    }

    public static SimpleAssemblyResolver LoadFromDirectory(string path)
    {
        var assemblyResolver = new SimpleAssemblyResolver();
        var moduleReaderParameters = new ModuleReaderParameters(Path.GetDirectoryName(path));

        foreach (var file in Directory.GetFiles(path))
        {
            if (Path.GetExtension(file) != ".dll") continue;

            var assemblyDefinition = AssemblyDefinition.FromImage(PEImage.FromFile(file), moduleReaderParameters);

            foreach (var module in assemblyDefinition.Modules)
            {
                module.MetadataResolver = new DefaultMetadataResolver(assemblyResolver);
            }

            assemblyResolver.Assemblies.Add(assemblyDefinition.Name!, assemblyDefinition);
        }

        return assemblyResolver;
    }
}
