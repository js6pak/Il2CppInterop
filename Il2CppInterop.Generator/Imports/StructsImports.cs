using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using AsmResolver.DotNet.Signatures.Types;
using Il2CppInterop.Generator.Extensions;

namespace Il2CppInterop.Generator.Imports;

internal sealed class StructsImports
{
    private const string Namespace = "Il2CppInterop.Bindings.Structs";

    public Il2CppClassImports Il2CppClass { get; }
    public Il2CppObjectImports Il2CppObject { get; }
    public Il2CppMethodImports Il2CppMethod { get; }
    public Il2CppStringImports Il2CppString { get; }
    public TypeWithPointerImports Il2CppArray { get; }

    public StructsImports(InteropImports imports)
    {
        Il2CppClass = new Il2CppClassImports(imports);
        Il2CppObject = new Il2CppObjectImports(imports);
        Il2CppMethod = new Il2CppMethodImports(imports);
        Il2CppString = new Il2CppStringImports(imports);
        Il2CppArray = new TypeWithPointerImports(imports, imports.Il2CppInteropBindings.CreateTypeReference(imports.Module, Namespace, "Il2CppArray"), true);
    }

    public void SetupMemberReferences()
    {
        Il2CppClass.SetupMemberReferences();
        Il2CppObject.SetupMemberReferences();
        Il2CppMethod.SetupMemberReferences();
        Il2CppString.SetupMemberReferences();
        Il2CppArray.SetupMemberReferences();
    }

    public sealed class Il2CppClassImports : TypeWithPointerImports
    {
        public MemberReference GetMethodByToken { get; private set; } = null!;

        public Il2CppClassImports(InteropImports imports) : base(imports, imports.Il2CppInteropBindings.CreateTypeReference(imports.Module, Namespace, "Il2CppClass"), true)
        {
        }

        public override void SetupMemberReferences()
        {
            GetMethodByToken = Type.CreateMemberReference("GetMethodByToken", MethodSignature.CreateInstance(
                Imports.Structs.Il2CppMethod.Pointer,
                Imports.CorLibFactory.UInt32 // token
            ));
        }
    }

    public sealed class Il2CppObjectImports : TypeWithPointerPointerImports
    {
        private MemberReference _genericUnbox = null!;

        public MemberReference New { get; private set; } = null!;
        public MemberReference Unbox { get; private set; } = null!;

        public MethodSpecification GenericUnbox(TypeSignature genericType)
        {
            return _genericUnbox.MakeGenericInstanceMethod(genericType);
        }

        public Il2CppObjectImports(InteropImports imports) : base(imports, imports.Il2CppInteropBindings.CreateTypeReference(imports.Module, Namespace, "Il2CppObject"), true)
        {
        }

        public override void SetupMemberReferences()
        {
            New = Type.CreateMemberReference("New", MethodSignature.CreateStatic(
                Pointer,
                Imports.Structs.Il2CppClass.Pointer // klass
            ));

            Unbox = Type.CreateMemberReference("Unbox", MethodSignature.CreateInstance(
                Imports.Void.Pointer
            ));

            _genericUnbox = new MemberReference(Type, "Unbox", MethodSignature.CreateInstance(
                new GenericParameterSignature(Imports.Module, GenericParameterType.Method, 0),
                1
            ));
        }
    }

    public sealed class Il2CppMethodImports : TypeWithPointerImports
    {
        public MemberReference Invoke { get; private set; } = null!;

        public MemberReference GetMethodPointer { get; private set; } = null!;

        public Il2CppMethodImports(InteropImports imports) : base(imports, imports.Il2CppInteropBindings.CreateTypeReference(imports.Module, Namespace, "Il2CppMethod"), true)
        {
        }

        public override void SetupMemberReferences()
        {
            Invoke = Type.CreateMemberReference("Invoke", MethodSignature.CreateInstance(
                Imports.Structs.Il2CppObject.Pointer,
                Imports.Void.Pointer, // obj
                Imports.Void.PointerPointer // params
            ));

            GetMethodPointer = Type.CreateMemberReference("get_MethodPointer", MethodSignature.CreateInstance(
                Imports.Void.Pointer
            ));
        }
    }

    public sealed class Il2CppStringImports : TypeWithPointerImports
    {
        public MemberReference From { get; private set; } = null!;

        public new MemberReference ToString { get; private set; } = null!;

        public Il2CppStringImports(InteropImports imports) : base(imports, imports.Il2CppInteropBindings.CreateTypeReference(imports.Module, Namespace, "Il2CppString"), true)
        {
        }

        public override void SetupMemberReferences()
        {
            From = Type.CreateMemberReference("From", MethodSignature.CreateStatic(
                Pointer,
                Imports.CorLibFactory.String // value
            ));

            ToString = Type.CreateMemberReference("ToString", MethodSignature.CreateInstance(
                Imports.CorLibFactory.String
            ));
        }
    }
}
