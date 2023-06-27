using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using AsmResolver.DotNet.Signatures.Types;
using Il2CppInterop.Generator.Extensions;

namespace Il2CppInterop.Generator.Interop;

public sealed class InteropImports
{
    private readonly ModuleDefinition _module;

    public ReferenceImporter Importer => _module.DefaultImporter;
    public CorLibTypeFactory CorLibFactory => _module.CorLibTypeFactory;

    public AssemblyReference Il2CppInteropRuntime { get; }
    public AssemblyReference Il2CppInteropBindings { get; }

    public ITypeDefOrRef Enum { get; }
    public ITypeDefOrRef ValueType { get; }
    public ITypeDefOrRef Type { get; }
    public ITypeDefOrRef RuntimeTypeHandle { get; }

    public ITypeDefOrRef RuntimeHelpers { get; }
    public MemberReference RunClassConstructor { get; }

    public StructsImports Structs { get; }
    public Il2CppRuntimeImports Il2CppRuntime { get; }
    public Il2CppClassPointerStoreImports Il2CppClassPointerStore { get; }
    public LightReflectionImports LightReflection { get; }
    public CodeGenImports CodeGen { get; }
    public Il2CppObjectBaseImports Il2CppObjectBase { get; }

    public InteropImports(ModuleDefinition module)
    {
        _module = module;

        module.AssemblyReferences.Add(Il2CppInteropRuntime = new AssemblyReference("Il2CppInterop.Runtime", typeof(InteropImports).Assembly.GetName().Version));
        module.AssemblyReferences.Add(Il2CppInteropBindings = new AssemblyReference("Il2CppInterop.Bindings", new Version(0, 1, 0, 0)));

        ValueType = module.CorLibTypeFactory.CorLibScope.CreateTypeReference(module, "System", "ValueType");
        Enum = module.CorLibTypeFactory.CorLibScope.CreateTypeReference(module, "System", "Enum");
        Type = module.CorLibTypeFactory.CorLibScope.CreateTypeReference(module, "System", "Type");
        RuntimeTypeHandle = module.CorLibTypeFactory.CorLibScope.CreateTypeReference(module, "System", "RuntimeTypeHandle");

        RuntimeHelpers = module.CorLibTypeFactory.CorLibScope.CreateTypeReference(module, "System.Runtime.CompilerServices", "RuntimeHelpers");
        RunClassConstructor = RuntimeHelpers.CreateMemberReference("RunClassConstructor", MethodSignature.CreateStatic(
            CorLibFactory.Void,
            RuntimeTypeHandle.ToTypeSignature() // type
        ));

        Structs = new StructsImports(this);
        Il2CppRuntime = new Il2CppRuntimeImports(this);
        Il2CppClassPointerStore = new Il2CppClassPointerStoreImports(this);
        LightReflection = new LightReflectionImports(this);
        CodeGen = new CodeGenImports(this);
        Il2CppObjectBase = new Il2CppObjectBaseImports(this);
    }

    public sealed class StructsImports
    {
        public ITypeDefOrRef Il2CppClass { get; }
        public PointerTypeSignature Il2CppClassPointer { get; }
        public MemberReference GetMethodByToken { get; }

        public ITypeDefOrRef Il2CppObject { get; }
        public PointerTypeSignature Il2CppObjectPointer { get; }
        public PointerTypeSignature Il2CppObjectPointerPointer { get; }
        public MemberReference Il2CppObjectFakeBox { get; }
        private readonly MemberReference _genericIl2CppObjectUnbox;

        public MethodSpecification Il2CppObjectUnbox(TypeSignature genericType)
        {
            return _genericIl2CppObjectUnbox.MakeGenericInstanceMethod(genericType);
        }

        public ITypeDefOrRef Il2CppMethod { get; }
        public PointerTypeSignature Il2CppMethodPointer { get; }
        public MemberReference Il2CppMethodInvoke { get; }

        public ITypeDefOrRef Il2CppString { get; }
        public PointerTypeSignature Il2CppStringPointer { get; }

        public ITypeDefOrRef Il2CppArray { get; }
        public PointerTypeSignature Il2CppArrayPointer { get; }

        public StructsImports(InteropImports imports)
        {
            Il2CppClass = imports.Il2CppInteropBindings.CreateTypeReference(imports._module, "Il2CppInterop.Bindings.Structs", "Il2CppClass");
            Il2CppClassPointer = Il2CppClass.MakePointerType();

            Il2CppObject = imports.Il2CppInteropBindings.CreateTypeReference(imports._module, "Il2CppInterop.Bindings.Structs", "Il2CppObject");
            Il2CppObjectPointer = Il2CppObject.MakePointerType();
            Il2CppObjectPointerPointer = Il2CppObjectPointer.MakePointerType();

            Il2CppMethod = imports.Il2CppInteropBindings.CreateTypeReference(imports._module, "Il2CppInterop.Bindings.Structs", "Il2CppMethod");
            Il2CppMethodPointer = Il2CppMethod.MakePointerType();

            Il2CppString = imports.Il2CppInteropBindings.CreateTypeReference(imports._module, "Il2CppInterop.Bindings.Structs", "Il2CppString");
            Il2CppStringPointer = Il2CppString.MakePointerType();

            Il2CppArray = imports.Il2CppInteropBindings.CreateTypeReference(imports._module, "Il2CppInterop.Bindings.Structs", "Il2CppArray");
            Il2CppArrayPointer = Il2CppArray.MakePointerType();

            GetMethodByToken = Il2CppClass.CreateMemberReference("GetMethodByToken", MethodSignature.CreateInstance(
                Il2CppMethodPointer,
                imports.CorLibFactory.UInt32 // token
            ));

            Il2CppObjectFakeBox = Il2CppObject.CreateMemberReference("FakeBox", MethodSignature.CreateStatic(
                Il2CppObjectPointer,
                imports.CorLibFactory.Void.MakePointerType() // data
            ));

            _genericIl2CppObjectUnbox = new MemberReference(Il2CppObject, "Unbox", MethodSignature.CreateInstance(
                new GenericParameterSignature(imports._module, GenericParameterType.Method, 0),
                1
            ));

            Il2CppMethodInvoke = Il2CppMethod.CreateMemberReference("Invoke", MethodSignature.CreateInstance(
                Il2CppObjectPointer,
                Il2CppObjectPointer, // obj
                Il2CppObjectPointerPointer // params
            ));
        }
    }

    public sealed class Il2CppRuntimeImports
    {
        public ITypeDefOrRef Type { get; }

        public MemberReference GetClassFromName { get; }
        public MemberReference GetNestedClassFromName { get; }
        public MemberReference ClassInit { get; }

        public Il2CppRuntimeImports(InteropImports imports)
        {
            Type = imports.Il2CppInteropBindings.CreateTypeReference(imports._module, "Il2CppInterop.Bindings", "Il2CppRuntime");

            GetClassFromName = Type.CreateMemberReference("GetClassFromName", MethodSignature.CreateStatic(
                imports.Structs.Il2CppClassPointer,
                imports.CorLibFactory.String, // imageName
                imports.CorLibFactory.String, // @namespace
                imports.CorLibFactory.String // name
            ));

            GetNestedClassFromName = Type.CreateMemberReference("GetNestedClassFromName", MethodSignature.CreateStatic(
                imports.Structs.Il2CppClassPointer,
                imports.Structs.Il2CppClassPointer, // declaringType
                imports.CorLibFactory.String // nestedTypeName
            ));

            ClassInit = Type.CreateMemberReference("ClassInit", MethodSignature.CreateStatic(
                imports.CorLibFactory.Void,
                imports.Structs.Il2CppClassPointer // klass
            ));
        }
    }

    public sealed class Il2CppClassPointerStoreImports
    {
        private readonly InteropImports _imports;

        public ITypeDefOrRef Type { get; }
        public ITypeDefOrRef GenericType { get; }

        public MemberReference Get { get; }
        public MemberReference Set { get; }

        public MemberReference GenericGet(TypeSignature genericType)
        {
            return new MemberReference(GenericType.MakeGenericInstanceType(genericType).ToTypeDefOrRef(), "get_Pointer", MethodSignature.CreateStatic(
                _imports.Structs.Il2CppClassPointer
            ));
        }

        public MemberReference GenericSet(TypeSignature genericType)
        {
            return new MemberReference(GenericType.MakeGenericInstanceType(genericType).ToTypeDefOrRef(), "set_Pointer", MethodSignature.CreateStatic(
                _imports.CorLibFactory.Void,
                _imports.Structs.Il2CppClassPointer
            ));
        }

        public Il2CppClassPointerStoreImports(InteropImports imports)
        {
            _imports = imports;
            Type = imports.Il2CppInteropBindings.CreateTypeReference(imports._module, "Il2CppInterop.Runtime", "Il2CppClassPointerStore");
            GenericType = imports.Il2CppInteropBindings.CreateTypeReference(imports._module, Type.Namespace, Type.Name + "`1");

            Get = Type.CreateMemberReference("Get", MethodSignature.CreateStatic(
                imports.Structs.Il2CppClassPointer,
                Type.ToTypeSignature() // type
            ));

            Set = Type.CreateMemberReference("Set", MethodSignature.CreateStatic(
                imports.CorLibFactory.Void,
                Type.ToTypeSignature(), // type
                imports.Structs.Il2CppClassPointer // value
            ));
        }
    }

    public sealed class LightReflectionImports
    {
        public ITypeDefOrRef Type { get; }

        public MemberReference MakeGenericClass { get; }
        public MemberReference MakeGenericMethod { get; }

        public LightReflectionImports(InteropImports imports)
        {
            Type = imports.Il2CppInteropBindings.CreateTypeReference(imports._module, "Il2CppInterop.Runtime", "LightReflection");

            MakeGenericClass = Type.CreateMemberReference("MakeGenericClass", MethodSignature.CreateStatic(
                imports.Structs.Il2CppClassPointer,
                imports.Structs.Il2CppClassPointer, // type
                new SzArrayTypeSignature(imports.Structs.Il2CppClassPointer) // classArguments
            ));

            MakeGenericMethod = Type.CreateMemberReference("MakeGenericMethod", MethodSignature.CreateStatic(
                imports.Structs.Il2CppMethodPointer,
                imports.Structs.Il2CppMethodPointer, // method
                new SzArrayTypeSignature(imports.Structs.Il2CppClassPointer) // classArguments
            ));
        }
    }

    public sealed class CodeGenImports
    {
        public ITypeDefOrRef Type { get; }

        public MemberReference EnsureValueTypeSize { get; }

        public CodeGenImports(InteropImports imports)
        {
            Type = imports.Il2CppInteropBindings.CreateTypeReference(imports._module, "Il2CppInterop.Runtime", "Il2CppCodeGen");
            EnsureValueTypeSize = new MemberReference(Type, "EnsureValueTypeSize", MethodSignature.CreateStatic(imports.CorLibFactory.Void, 1));
        }
    }

    public sealed class Il2CppObjectBaseImports
    {
        public ITypeDefOrRef Type { get; }

        public MemberReference GetPointer { get; }

        public Il2CppObjectBaseImports(InteropImports imports)
        {
            Type = imports.Il2CppInteropBindings.CreateTypeReference(imports._module, "Il2CppInterop.Bindings.InteropTypes", "Il2CppObjectBase");

            GetPointer = Type.CreateMemberReference("get_Pointer", MethodSignature.CreateInstance(
                imports.Structs.Il2CppObjectPointer
            ));
        }
    }
}
