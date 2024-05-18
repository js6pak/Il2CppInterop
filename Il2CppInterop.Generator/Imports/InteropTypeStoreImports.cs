using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using Il2CppInterop.Generator.Extensions;

namespace Il2CppInterop.Generator.Imports;

internal sealed class InteropTypeStoreImports : TypeImports
{
    public MemberReference Get { get; private set; } = null!;
    public MemberReference Set { get; private set; } = null!;

    public InteropTypeStoreImports(InteropImports imports) : base(imports, imports.Il2CppInteropRuntime.CreateTypeReference(imports.Module, "Il2CppInterop.Runtime.InteropTypes.Stores", "InteropTypeStore"))
    {
    }

    public override void SetupMemberReferences()
    {
        Get = Type.CreateMemberReference("Get", MethodSignature.CreateStatic(
            Imports.Type.ToTypeSignature(),
            Imports.Structs.Il2CppClass.Pointer // klass
        ));

        Set = Type.CreateMemberReference("Set", MethodSignature.CreateStatic(
            Imports.CorLibFactory.Void,
            Imports.Structs.Il2CppClass.Pointer, // klass
            Imports.Type.ToTypeSignature() // interopType
        ));
    }
}
