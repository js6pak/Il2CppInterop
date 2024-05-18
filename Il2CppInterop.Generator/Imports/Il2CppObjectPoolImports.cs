using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using AsmResolver.DotNet.Signatures.Types;
using Il2CppInterop.Generator.Extensions;

namespace Il2CppInterop.Generator.Imports;

internal sealed class Il2CppObjectPoolImports : TypeImports
{
    public MemberReference Get { get; private set; } = null!;

    private MemberReference _genericGet = null!;

    public MethodSpecification GenericGet(TypeSignature genericType)
    {
        return _genericGet.MakeGenericInstanceMethod(genericType);
    }

    public Il2CppObjectPoolImports(InteropImports imports) : base(imports, imports.Il2CppInteropRuntime.CreateTypeReference(imports.Module, "Il2CppInterop.Runtime.InteropTypes", "Il2CppObjectPool"))
    {
    }

    public override void SetupMemberReferences()
    {
        Get = Type.CreateMemberReference("Get", MethodSignature.CreateStatic(
            Imports.Il2CppObjectBase.Type.ToTypeSignature(),
            Imports.Structs.Il2CppObject.Pointer // pointer
        ));

        _genericGet = new MemberReference(Type, "Get", MethodSignature.CreateStatic(
            new GenericParameterSignature(Imports.Module, GenericParameterType.Method, 0),
            1,
            Imports.Structs.Il2CppObject.Pointer // pointer
        ));
    }
}
