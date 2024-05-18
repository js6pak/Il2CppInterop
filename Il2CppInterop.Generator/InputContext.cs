using AsmResolver.DotNet;

namespace Il2CppInterop.Generator;

public sealed class InputContext
{
    public IEnumerable<AssemblyDefinition> Assemblies { get; }

    public InputContext(IEnumerable<AssemblyDefinition> assemblies)
    {
        Assemblies = assemblies;
    }

    public static InputContext LoadFromDirectory(string path)
    {
        var simpleAssemblyResolver = SimpleAssemblyResolver.LoadFromDirectory(path);
        return new InputContext(simpleAssemblyResolver.Assemblies.Values);
    }
}
