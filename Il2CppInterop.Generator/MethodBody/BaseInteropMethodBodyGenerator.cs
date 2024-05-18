using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.PE.DotNet.Cil;
using Il2CppInterop.Generator.Contexts.Interop;

namespace Il2CppInterop.Generator.MethodBody;

internal abstract class BaseInteropMethodBodyGenerator
{
    protected static void GenerateBaseCall(InteropMethodContext context, CilInstructionCollection instructions)
    {
        var imports = context.Imports;

        // base(Il2CppObject.New(Il2CppClassPointerStore<{this}>.Pointer));
        instructions.Add(CilOpCodes.Ldarg_0);
        instructions.Add(CilOpCodes.Call, imports.Il2CppClassPointerStore.GenericGet(context.DeclaringType.Definition.ToTypeSignature()));
        instructions.Add(CilOpCodes.Call, imports.Structs.Il2CppObject.New);
        instructions.Add(CilOpCodes.Call, imports.Il2CppObjectBase.Constructor);
    }

    protected static void GenerateLoadThisPointer(InteropMethodContext context, CilInstructionCollection instructions)
    {
        var imports = context.Imports;

        if (context.DeclaringType.Definition.IsValueType)
        {
            // &this
            instructions.Add(CilOpCodes.Ldarg_0);
        }
        else
        {
            // this.Pointer
            instructions.Add(CilOpCodes.Ldarg_0);
            instructions.Add(CilOpCodes.Call, imports.Il2CppObjectBase.GetPointer);
        }
    }

    public abstract void GenerateBody(InteropMethodContext context, MethodDefinition definition, CilMethodBody body);
}
