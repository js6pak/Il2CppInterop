using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using AsmResolver.DotNet.Signatures.Types;
using Il2CppInterop.Generator.Extensions;

namespace Il2CppInterop.Generator.Imports;

internal sealed class InteropImports
{
    public ModuleDefinition Module { get; }

    public CorLibTypeFactory CorLibFactory => Module.CorLibTypeFactory;
    public IResolutionScope CorLibScope => CorLibFactory.CorLibScope;

    public AssemblyReference Il2CppInteropRuntime { get; }
    public AssemblyReference Il2CppInteropBindings { get; }

    public TypeWithPointerPointerImports Void { get; }
    public ITypeDefOrRef Object { get; }
    public ITypeDefOrRef ValueType { get; }
    public ITypeDefOrRef Enum { get; }
    public ITypeDefOrRef Type { get; }
    public ITypeDefOrRef Attribute { get; }
    public MemberReference GetTypeFromHandle { get; }
    public ITypeDefOrRef RuntimeTypeHandle { get; }
    public MemberReference StringGetLength { get; }

    public RuntimeHelpersImports RuntimeHelpers { get; }

    public StructsImports Structs { get; }
    public Il2CppRuntimeImports Il2CppRuntime { get; }
    public Il2CppClassPointerStoreImports Il2CppClassPointerStore { get; }
    public InteropTypeStoreImports InteropTypeStore { get; }
    public LightReflectionImports LightReflection { get; }
    public CodeGenImports CodeGen { get; }
    public Il2CppObjectBaseImports Il2CppObjectBase { get; }
    public Il2CppObjectPoolImports Il2CppObjectPool { get; }

    public InteropImports(ModuleDefinition module)
    {
        Module = module;

        Il2CppInteropRuntime = module.CreateAssemblyReference("Il2CppInterop.Runtime", Constants.Il2CppInteropRuntimeVersion);
        Il2CppInteropBindings = module.CreateAssemblyReference("Il2CppInterop.Bindings", Constants.Il2CppInteropBindingsVersion);

        Void = new TypeWithPointerPointerImports(this, CorLibFactory.Void.Type, true);
        Object = CorLibScope.CreateTypeReference(module, "System", "Object");
        ValueType = CorLibScope.CreateTypeReference(module, "System", "ValueType");
        Enum = CorLibScope.CreateTypeReference(module, "System", "Enum");
        Type = CorLibScope.CreateTypeReference(module, "System", "Type");
        Attribute = CorLibScope.CreateTypeReference(module, "System", "Attribute");
        RuntimeTypeHandle = CorLibScope.CreateTypeReference(module, "System", "RuntimeTypeHandle");
        GetTypeFromHandle = Type.CreateMemberReference("GetTypeFromHandle", MethodSignature.CreateStatic(
            Type.ToTypeSignature(),
            RuntimeTypeHandle.ToTypeSignature(true) // handle
        ));
        StringGetLength = CorLibFactory.String.Type.CreateMemberReference("get_Length", MethodSignature.CreateInstance(
            CorLibFactory.Int32
        ));

        RuntimeHelpers = new RuntimeHelpersImports(this);

        Structs = new StructsImports(this);
        Il2CppRuntime = new Il2CppRuntimeImports(this);
        Il2CppClassPointerStore = new Il2CppClassPointerStoreImports(this);
        InteropTypeStore = new InteropTypeStoreImports(this);
        LightReflection = new LightReflectionImports(this);
        CodeGen = new CodeGenImports(this);
        Il2CppObjectBase = new Il2CppObjectBaseImports(this);
        Il2CppObjectPool = new Il2CppObjectPoolImports(this);

        SetupMemberReferences();
    }

    public void SetupMemberReferences()
    {
        Structs.SetupMemberReferences();
        Il2CppRuntime.SetupMemberReferences();
        Il2CppClassPointerStore.SetupMemberReferences();
        InteropTypeStore.SetupMemberReferences();
        LightReflection.SetupMemberReferences();
        CodeGen.SetupMemberReferences();
        Il2CppObjectBase.SetupMemberReferences();
        Il2CppObjectPool.SetupMemberReferences();
    }
}
