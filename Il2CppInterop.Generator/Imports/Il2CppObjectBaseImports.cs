using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using Il2CppInterop.Generator.Extensions;

namespace Il2CppInterop.Generator.Imports;

internal sealed class Il2CppObjectBaseImports : TypeImports
{
    public MemberReference Constructor { get; private set; } = null!;
    public MemberReference GetPointer { get; private set; } = null!;

    public Il2CppObjectBaseImports(InteropImports imports) : base(imports, imports.Il2CppInteropRuntime.CreateTypeReference(imports.Module, "Il2CppInterop.Runtime.InteropTypes", "Il2CppObjectBase"))
    {
    }

    public override void SetupMemberReferences()
    {
        Constructor = Type.CreateMemberReference(".ctor", MethodSignature.CreateInstance(
            Imports.CorLibFactory.Void,
            Imports.Structs.Il2CppObject.Pointer
        ));

        GetPointer = Type.CreateMemberReference("get_Pointer", MethodSignature.CreateInstance(
            Imports.Structs.Il2CppObject.Pointer
        ));
    }
}
