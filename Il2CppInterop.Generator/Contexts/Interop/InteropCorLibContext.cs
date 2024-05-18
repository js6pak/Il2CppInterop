using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.DotNet.Signatures;
using AsmResolver.DotNet.Signatures.Types;
using AsmResolver.PE.DotNet.Cil;
using AsmResolver.PE.DotNet.Metadata.Tables.Rows;
using Il2CppInterop.Generator.Extensions;

namespace Il2CppInterop.Generator.Contexts.Interop;

internal sealed class InteropCorLibContext : InteropAssemblyContext
{
    public InteropCorLibContext(InteropGenerationContext generationContext, AssemblyDefinition originalAssembly, SimpleAssemblyResolver assemblyResolver) : base(generationContext, originalAssembly, assemblyResolver)
    {
    }

    public InteropTypeContext Object { get; private set; } = null!;

    public InteropTypeContext String { get; private set; } = null!;

    public bool IsString(TypeSignature signature) =>
        signature is TypeDefOrRefSignature typeDefOrRefSignature &&
        SignatureComparer.Default.Equals(String.Definition, typeDefOrRefSignature.Type);

    public override void CreateEmptyTypeDefinitions()
    {
        base.CreateEmptyTypeDefinitions();

        Object = FindType("Il2Cpp.System", "Object") ?? throw new InvalidOperationException("Failed to find System.Object");
        String = FindType("Il2Cpp.System", "String") ?? throw new InvalidOperationException("Failed to find System.String");
    }

    public override void Setup()
    {
        base.Setup();

        Object.Definition.BaseType = Imports.Il2CppObjectBase.Type;
    }

    public override void Fill()
    {
        base.Fill();

        AddStringConversions();
    }

    private void AddStringConversions()
    {
        var il2CppString = String.Definition;
        var systemString = Imports.CorLibFactory.String;

        var fromMethod = AsmResolverExtensions.CreateImplicitConversion(il2CppString.ToTypeSignature(), systemString);
        {
            var body = fromMethod.CilMethodBody = new CilMethodBody(fromMethod);
            var instructions = body.Instructions;

            // if (value == null) return null;
            instructions.AddConditionalReturn(() =>
            {
                instructions.Add(CilOpCodes.Ldarg_0);
            }, () =>
            {
                instructions.Add(CilOpCodes.Ldnull);
            }, true);

            // if (value.Length == 0) return Il2Cpp.System.String.Empty;
            // TODO readd after fields are working
            // instructions.AddConditionalReturn(() =>
            // {
            //     instructions.Add(CilOpCodes.Ldarg_0);
            //     instructions.Add(CilOpCodes.Callvirt, Imports.StringGetLength);
            // }, () =>
            // {
            //     instructions.Add(CilOpCodes.Call, il2CppString.CreateMemberReference("get_Empty", MethodSignature.CreateStatic(
            //         il2CppString.ToTypeSignature()
            //     )));
            // }, true);

            // return Il2CppObjectPool.Get<Il2Cpp.System.String>((Il2CppObject*)Il2CppString.From(value));
            instructions.Add(CilOpCodes.Ldarg_0);
            instructions.Add(CilOpCodes.Call, Imports.Structs.Il2CppString.From);
            instructions.Add(CilOpCodes.Call, Imports.Il2CppObjectPool.GenericGet(il2CppString.ToTypeSignature()));
            instructions.Add(CilOpCodes.Ret);
        }
        il2CppString.Methods.Add(fromMethod);

        var intoMethod = AsmResolverExtensions.CreateImplicitConversion(systemString, il2CppString.ToTypeSignature());
        {
            var body = intoMethod.CilMethodBody = new CilMethodBody(intoMethod);
            var instructions = body.Instructions;

            // if (value == null) return null;
            instructions.AddConditionalReturn(() =>
            {
                instructions.Add(CilOpCodes.Ldarg_0);
            }, () =>
            {
                instructions.Add(CilOpCodes.Ldnull);
            }, true);

            // return ((Il2CppString*)value.Pointer)->ToString();
            instructions.Add(CilOpCodes.Ldarg_0);
            instructions.Add(CilOpCodes.Call, Imports.Il2CppObjectBase.GetPointer);
            instructions.Add(CilOpCodes.Call, Imports.Structs.Il2CppString.ToString);
            instructions.Add(CilOpCodes.Ret);
        }
        il2CppString.Methods.Add(intoMethod);
    }
}
