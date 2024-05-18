using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using AsmResolver.DotNet.Signatures.Types;
using Il2CppInterop.Generator.Extensions;

namespace Il2CppInterop.Generator.Imports;

internal sealed class LightReflectionImports : TypeImports
{
    public MemberReference MakeGenericClass { get; private set; } = null!;
    public MemberReference MakeGenericMethod { get; private set; } = null!;

    public LightReflectionImports(InteropImports imports) : base(imports, imports.Il2CppInteropRuntime.CreateTypeReference(imports.Module, "Il2CppInterop.Runtime", "LightReflection"))
    {
    }

    public override void SetupMemberReferences()
    {
        MakeGenericClass = Type.CreateMemberReference("MakeGenericClass", MethodSignature.CreateStatic(
            Imports.Structs.Il2CppClass.Pointer,
            Imports.Structs.Il2CppClass.Pointer, // type
            new SzArrayTypeSignature(Imports.Structs.Il2CppClass.Pointer) // classArguments
        ));

        MakeGenericMethod = Type.CreateMemberReference("MakeGenericMethod", MethodSignature.CreateStatic(
            Imports.Structs.Il2CppMethod.Pointer,
            Imports.Structs.Il2CppMethod.Pointer, // method
            new SzArrayTypeSignature(Imports.Structs.Il2CppClass.Pointer) // classArguments
        ));
    }
}
