using Il2CppInterop.Generator.Interop;

namespace Il2CppInterop.Generator;

public static class InteropAssemblyGenerator
{
    public static void Run(GeneratorOptions options)
    {
        var generationContext = new InteropGenerationContext(options.Input);

        // Pass 0: create all type definitions so they can be referenced later
        generationContext.CreateAssembliesAndEmptyTypeDefinitions();

        // Pass 1: setup type definitions (base type, interfaces, generics)
        foreach (var typeContext in generationContext.TopLevelTypes)
        {
            typeContext.Setup();
        }

        // Pass 2: fill type definitions (methods, fields, static constructor)
        foreach (var typeContext in generationContext.TopLevelTypes)
        {
            typeContext.Fill();
        }

        Directory.CreateDirectory(options.OutputPath);
        generationContext.Save(options.OutputPath);
    }
}
