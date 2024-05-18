using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using AsmResolver.DotNet.Signatures.Types;
using Il2CppInterop.Generator.Extensions;

namespace Il2CppInterop.Generator.Imports;

internal sealed class Il2CppClassPointerStoreImports : TypeImports
{
    public ITypeDefOrRef GenericType { get; }

    public MemberReference Get { get; private set; } = null!;
    public MemberReference Set { get; private set; } = null!;

    public MemberReference GenericGet(TypeSignature genericType)
    {
        return new MemberReference(GenericType.MakeGenericInstanceType(genericType).ToTypeDefOrRef(), "get_Pointer", MethodSignature.CreateStatic(
            Imports.Structs.Il2CppClass.Pointer
        ));
    }

    public MemberReference GenericSet(TypeSignature genericType)
    {
        return new MemberReference(GenericType.MakeGenericInstanceType(genericType).ToTypeDefOrRef(), "set_Pointer", MethodSignature.CreateStatic(
            Imports.CorLibFactory.Void,
            Imports.Structs.Il2CppClass.Pointer
        ));
    }

    public Il2CppClassPointerStoreImports(InteropImports imports) : base(imports, imports.Il2CppInteropRuntime.CreateTypeReference(imports.Module, "Il2CppInterop.Runtime.InteropTypes.Stores", "Il2CppClassPointerStore"))
    {
        GenericType = imports.Il2CppInteropRuntime.CreateTypeReference(imports.Module, Type.Namespace, Type.Name + "`1");
    }

    public override void SetupMemberReferences()
    {
        Get = Type.CreateMemberReference("Get", MethodSignature.CreateStatic(
            Imports.Structs.Il2CppClass.Pointer,
            Imports.Type.ToTypeSignature() // type
        ));

        Set = Type.CreateMemberReference("Set", MethodSignature.CreateStatic(
            Imports.CorLibFactory.Void,
            Imports.Type.ToTypeSignature(), // type
            Imports.Structs.Il2CppClass.Pointer // value
        ));
    }
}
