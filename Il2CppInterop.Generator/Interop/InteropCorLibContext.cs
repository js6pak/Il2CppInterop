using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using AsmResolver.DotNet.Signatures.Types;

namespace Il2CppInterop.Generator.Interop;

public sealed class InteropCorLibContext : InteropAssemblyContext
{
    public InteropCorLibContext(InteropGenerationContext generationContext, AssemblyDefinition originalAssembly, SimpleAssemblyResolver assemblyResolver) : base(generationContext, originalAssembly, assemblyResolver)
    {
    }

    public InteropTypeContext String { get; private set; } = null!;

    public bool IsString(TypeSignature signature) =>
        signature is TypeDefOrRefSignature typeDefOrRefSignature &&
        SignatureComparer.Default.Equals(String.Definition, typeDefOrRefSignature.Type);

    public override void CreateEmptyTypeDefinitions()
    {
        base.CreateEmptyTypeDefinitions();

        String = FindType("Il2Cpp.System", "String") ?? throw new InvalidOperationException("Failed to find System.String");
    }
}
