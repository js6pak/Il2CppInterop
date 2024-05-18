using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.DotNet.Signatures;
using AsmResolver.DotNet.Signatures.Types;
using AsmResolver.PE.DotNet.Cil;
using Il2CppInterop.Generator.Contexts.Interop;
using Il2CppInterop.Generator.Extensions;
using Il2CppInterop.Generator.Imports;

namespace Il2CppInterop.Generator.MethodBody;

internal sealed class DirectInteropMethodBodyGenerator : BaseInteropMethodBodyGenerator
{
    private static TypeSignature ConvertType(InteropImports imports, ITypeDescriptor type, bool wrapValueTypeInPointers = false)
    {
        if (type is ByReferenceTypeSignature byReferenceTypeSignature)
        {
            return ConvertType(imports, byReferenceTypeSignature.BaseType).MakePointerType();
        }

        if (type.IsValueTypeLike())
        {
            return wrapValueTypeInPointers ? type.MakePointerType() : type.ToTypeSignature();
        }

        return imports.Structs.Il2CppObject.Pointer;
    }

    private static FunctionPointerTypeSignature CreateFunctionPointerTypeSignature(InteropMethodContext context, MethodSignature signature)
    {
        var imports = context.Imports;
        var methodDefinition = context.Definition!;

        var returnType = ConvertType(imports, signature.ReturnType);
        var parameterTypes = new List<TypeSignature>();

        if (!methodDefinition.IsStatic)
        {
            parameterTypes.Add(ConvertType(imports, context.DeclaringType.Definition, true));
        }

        foreach (var parameterType in signature.ParameterTypes)
        {
            parameterTypes.Add(ConvertType(imports, parameterType));
        }

        parameterTypes.Add(imports.Structs.Il2CppMethod.Pointer);

        return new FunctionPointerTypeSignature(new MethodSignature(CallingConventionAttributes.Unmanaged, returnType, parameterTypes));
    }

    public override void GenerateBody(InteropMethodContext context, MethodDefinition definition, CilMethodBody body)
    {
        var imports = context.Imports;
        var instructions = body.Instructions;
        var signature = definition.Signature!;

        if (definition.IsConstructor)
        {
            GenerateBaseCall(context, instructions);
        }

        var functionPointerTypeSignature = CreateFunctionPointerTypeSignature(context, signature);

        var methodLocal = new CilLocalVariable(functionPointerTypeSignature);
        body.LocalVariables.Add(methodLocal);

        // var method = (delegate *unmanaged<{functionPointerTypeSignature}>)Il2CppMethodInfo_{token}->MethodPointer;
        instructions.Add(CilOpCodes.Ldsfld, context.MethodInfoField);
        instructions.Add(CilOpCodes.Call, imports.Structs.Il2CppMethod.GetMethodPointer);
        instructions.Add(CilOpCodes.Stloc, methodLocal);

        if (!definition.IsStatic)
        {
            // this.Pointer or &this
            GenerateLoadThisPointer(context, instructions);
        }

        foreach (var parameter in definition.Parameters)
        {
            if (parameter.ParameterType.IsValueType || parameter.ParameterType is PointerTypeSignature || parameter.ParameterType is ByReferenceTypeSignature)
            {
                // {parameter}
                instructions.Add(CilOpCodes.Ldarg, parameter);
            }
            else
            {
                // {parameter} != null ? {parameter}.Pointer : null;
                instructions.AddIfStatement(() =>
                {
                    instructions.Add(CilOpCodes.Ldarg, parameter);
                }, () =>
                {
                    instructions.Add(CilOpCodes.Ldarg, parameter);
                    instructions.Add(CilOpCodes.Call, imports.Il2CppObjectBase.GetPointer);
                }, () =>
                {
                    instructions.Add(CilOpCodes.Ldc_I4, 0);
                    instructions.Add(CilOpCodes.Conv_U);
                });
            }
        }

        // Il2CppMethodInfo_{token}
        instructions.Add(CilOpCodes.Ldsfld, context.MethodInfoField);

        // method(^...)
        instructions.Add(CilOpCodes.Ldloc, methodLocal);
        instructions.Add(CilOpCodes.Calli, functionPointerTypeSignature.Signature.MakeStandAloneSignature());

        if (!signature.ReturnType.IsValueTypeLike())
        {
            // return Il2CppObjectPool.Get<{returnType}>(^);
            instructions.Add(CilOpCodes.Call, imports.Il2CppObjectPool.GenericGet(signature.ReturnType));
        }

        instructions.Add(CilOpCodes.Ret);
        body.OptimizeAndVerify();
    }
}
