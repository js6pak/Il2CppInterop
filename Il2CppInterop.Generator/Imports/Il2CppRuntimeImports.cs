using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using Il2CppInterop.Generator.Extensions;

namespace Il2CppInterop.Generator.Imports;

internal sealed class Il2CppRuntimeImports : TypeImports
{
    public MemberReference GetClassFromName { get; private set; } = null!;
    public MemberReference GetNestedClassFromName { get; private set; } = null!;
    public MemberReference ClassInit { get; private set; } = null!;

    public Il2CppRuntimeImports(InteropImports imports) : base(imports, imports.Il2CppInteropBindings.CreateTypeReference(imports.Module, "Il2CppInterop.Bindings", "Il2CppRuntime"))
    {
    }

    public override void SetupMemberReferences()
    {
        GetClassFromName = Type.CreateMemberReference("GetClassFromName", MethodSignature.CreateStatic(
            Imports.Structs.Il2CppClass.Pointer,
            Imports.CorLibFactory.String, // imageName
            Imports.CorLibFactory.String, // @namespace
            Imports.CorLibFactory.String // name
        ));

        GetNestedClassFromName = Type.CreateMemberReference("GetNestedClassFromName", MethodSignature.CreateStatic(
            Imports.Structs.Il2CppClass.Pointer,
            Imports.Structs.Il2CppClass.Pointer, // declaringType
            Imports.CorLibFactory.String // nestedTypeName
        ));

        ClassInit = Type.CreateMemberReference("ClassInit", MethodSignature.CreateStatic(
            Imports.CorLibFactory.Void,
            Imports.Structs.Il2CppClass.Pointer // klass
        ));
    }
}
