using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.DotNet.Signatures.Types;
using AsmResolver.PE.DotNet.Cil;
using AsmResolver.PE.DotNet.Metadata.Tables.Rows;
using Il2CppInterop.Generator.Contexts.Interop;
using Il2CppInterop.Generator.Extensions;

namespace Il2CppInterop.Generator.MethodBody;

internal sealed class RuntimeInvokeInteropMethodBodyGenerator : BaseInteropMethodBodyGenerator
{
    public override void GenerateBody(InteropMethodContext context, MethodDefinition definition, CilMethodBody body)
    {
        var imports = context.Imports;
        var instructions = body.Instructions;

        if (definition.IsConstructor)
        {
            GenerateBaseCall(context, instructions);
        }

        CilLocalVariable? paramsLocal = null;
        var parameters = definition.Parameters;
        if (parameters.Count != 0)
        {
            paramsLocal = new CilLocalVariable(imports.Void.PointerPointer);
            body.LocalVariables.Add(paramsLocal);

            // void** @params = stackalloc void*[{parameters.Count}];
            instructions.AddStackAlloc(imports.Void.Pointer.ToTypeDefOrRef(), parameters.Count);
            instructions.Add(CilOpCodes.Stloc, paramsLocal);

            for (var i = 0; i < parameters.Count; i++)
            {
                var parameter = parameters[i];

                // @params[{i}] =
                instructions.Add(CilOpCodes.Ldloc, paramsLocal);
                if (i != 0)
                {
                    if (i > 1)
                    {
                        instructions.Add(CilOpCodes.Ldc_I4, i);
                        instructions.Add(CilOpCodes.Conv_I);
                    }

                    instructions.Add(CilOpCodes.Sizeof, imports.Void.Pointer.ToTypeDefOrRef());

                    if (i > 1)
                    {
                        instructions.Add(CilOpCodes.Mul);
                    }

                    instructions.Add(CilOpCodes.Add);
                }

                if (parameter.ParameterType.IsPointerLike())
                {
                    // {parameter};
                    instructions.Add(CilOpCodes.Ldarg, parameter);
                }
                else if (parameter.ParameterType.IsValueType)
                {
                    // &{parameter};
                    instructions.Add(CilOpCodes.Ldarga, parameter);
                    instructions.Add(CilOpCodes.Conv_U);
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

                instructions.Add(CilOpCodes.Stind_I);
            }
        }

        // Il2CppMethodInfo_{token}
        instructions.Add(CilOpCodes.Ldsfld, context.MethodInfoField);

        if (!definition.IsStatic)
        {
            // this.Pointer or &this
            GenerateLoadThisPointer(context, instructions);
        }
        else
        {
            // (Il2CppObject*)null
            instructions.Add(CilOpCodes.Ldc_I4_0);
            instructions.Add(CilOpCodes.Conv_U);
        }

        if (paramsLocal != null)
        {
            // @params
            instructions.Add(CilOpCodes.Ldloc, paramsLocal);
        }
        else
        {
            // (void**)null
            instructions.Add(CilOpCodes.Ldc_I4_0);
            instructions.Add(CilOpCodes.Conv_U);
        }

        // ^->Invoke(^, ^)
        instructions.Add(CilOpCodes.Call, imports.Structs.Il2CppMethod.Invoke);

        var returnType = definition.Signature!.ReturnType;
        if (returnType.ElementType == ElementType.Void)
        {
            // ;
            instructions.Add(CilOpCodes.Pop);
        }
        else if (returnType.IsPointerLike())
        {
            // Pointers are returned directly, for some reason
        }
        else if (returnType.IsValueType)
        {
            // return ^->Unbox<{returnType}>();
            instructions.Add(CilOpCodes.Call, imports.Structs.Il2CppObject.GenericUnbox(returnType));
        }
        else
        {
            // return Il2CppObjectPool.Get<{returnType}>(^);
            instructions.Add(CilOpCodes.Call, imports.Il2CppObjectPool.GenericGet(returnType));
        }

        instructions.Add(CilOpCodes.Ret);
        body.OptimizeAndVerify();
    }
}
