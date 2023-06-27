using AsmResolver.DotNet;

namespace Il2CppInterop.Generator;

public sealed class InputContext
{
    public IReadOnlyCollection<AssemblyDefinition> Assemblies { get; }

    public InputContext(IReadOnlyCollection<AssemblyDefinition> assemblies)
    {
        Assemblies = assemblies;
    }

    public static InputContext LoadFromDirectory(string path)
    {
        var simpleAssemblyResolver = SimpleAssemblyResolver.LoadFromDirectory(path);
        return new InputContext(simpleAssemblyResolver.Assemblies.Values);
    }
}
