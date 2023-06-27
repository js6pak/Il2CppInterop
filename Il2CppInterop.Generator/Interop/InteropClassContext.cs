using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.DotNet.Signatures;
using AsmResolver.DotNet.Signatures.Types;
using AsmResolver.PE.DotNet.Cil;
using AsmResolver.PE.DotNet.Metadata.Tables.Rows;
using Il2CppInterop.Generator.Extensions;

namespace Il2CppInterop.Generator.Interop;

public class InteropClassContext : InteropTypeContext
{
    public List<InteropMethod> Methods { get; } = new();

    public InteropClassContext(InteropAssemblyContext assembly, TypeDefinition original, InteropTypeContext? declaringType = null) : base(assembly, original, declaringType)
    {
        foreach (var originalMethod in Original.Methods)
        {
            if (originalMethod.Name == ".cctor") continue;

            var interopMethod = new InteropMethod(this, originalMethod);
            Methods.Add(interopMethod);
        }
    }

    public override void Setup()
    {
        base.Setup();

        foreach (var interopMethod in Methods)
        {
            interopMethod.Setup();
        }

        SetupStaticConstructor();
    }

    private void SetupStaticConstructor()
    {
        var methodDefinition = AsmResolverExtensions.CreateStaticConstructor(Module);

        var body = methodDefinition.CilMethodBody = new CilMethodBody(methodDefinition);

        var klassLocal = new CilLocalVariable(Imports.Structs.Il2CppClassPointer);
        body.LocalVariables.Add(klassLocal);

        // Il2CppClass* klass = Il2CppClassPointerStore<{this}>.Pointer =

        if (DeclaringType != null)
        {
            // Il2CppRuntime.GetNestedClassFromName(Il2CppClassPointerStore<{DeclaringType}>.Pointer, "{Name}");
            body.Instructions.Add(CilOpCodes.Call, Imports.Il2CppClassPointerStore.GenericGet(DeclaringType.Definition.ToTypeSignature()));
            body.Instructions.Add(CilOpCodes.Ldstr, DeclaringType.Original.Name);
            body.Instructions.Add(CilOpCodes.Call, Imports.Il2CppRuntime.GetNestedClassFromName);
        }
        else
        {
            // Il2CppRuntime.GetClassFromName({Module.Name}, {Namespace}, {Name});
            body.Instructions.Add(CilOpCodes.Ldstr, Assembly.OriginalModule.Name);
            if (Original.Namespace is null) body.Instructions.Add(CilOpCodes.Ldnull);
            else body.Instructions.Add(CilOpCodes.Ldstr, Original.Namespace);
            body.Instructions.Add(CilOpCodes.Ldstr, Original.Name);
            body.Instructions.Add(CilOpCodes.Call, Imports.Il2CppRuntime.GetClassFromName);
        }

        if (Original.GenericParameters.Any())
        {
            body.Instructions.Add(CilOpCodes.Ldc_I4, Original.GenericParameters.Count);
            body.Instructions.Add(CilOpCodes.Newarr, Imports.Structs.Il2CppClassPointer.ToTypeDefOrRef());

            for (var i = 0; i < Original.GenericParameters.Count; i++)
            {
                body.Instructions.Add(CilOpCodes.Dup);
                body.Instructions.Add(CilOpCodes.Ldc_I4, i);
                body.Instructions.Add(CilOpCodes.Call, Imports.Il2CppClassPointerStore.GenericGet(new GenericParameterSignature(GenericParameterType.Type, i)));
                body.Instructions.Add(CilOpCodes.Stelem_I);
            }

            body.Instructions.Add(CilOpCodes.Call, Imports.LightReflection.MakeGenericClass);
        }

        body.Instructions.Add(CilOpCodes.Dup);
        body.Instructions.Add(CilOpCodes.Call, Imports.Il2CppClassPointerStore.GenericSet(Definition.ToTypeSignature()));
        body.Instructions.Add(CilOpCodes.Stloc, klassLocal);

        // Il2CppRuntime.ClassInit(klass);
        body.Instructions.Add(CilOpCodes.Ldloc, klassLocal);
        body.Instructions.Add(CilOpCodes.Call, Imports.Il2CppRuntime.ClassInit);

        ConfigureStaticConstructorBody(body, klassLocal);

        body.Instructions.Add(CilOpCodes.Ret);

        body.Instructions.OptimizeMacros();

        Definition.Methods.Add(methodDefinition);
    }

    protected virtual void ConfigureStaticConstructorBody(CilMethodBody body, CilLocalVariable klassLocal)
    {
        foreach (var method in Methods)
        {
            // Il2CppMethodInfo_{token} = klass->GetMethodByToken({token});
            body.Instructions.Add(CilOpCodes.Ldloc, klassLocal);
            body.Instructions.Add(CilOpCodes.Ldc_I4, (int)method.Token);
            body.Instructions.Add(CilOpCodes.Call, Imports.Structs.GetMethodByToken);
            body.Instructions.Add(CilOpCodes.Stsfld, method.MethodInfoField);
        }
    }

    public sealed class InteropMethod
    {
        public InteropTypeContext DeclaringType { get; }
        public InteropImports Imports => DeclaringType.Imports;
        public MethodDefinition Original { get; }
        public uint Token { get; }
        public FieldDefinition MethodInfoField { get; }

        public MethodDefinition? Definition { get; private set; }

        public InteropMethod(InteropClassContext declaringType, MethodDefinition original)
        {
            DeclaringType = declaringType;
            Original = original;
            Token = Original.ExtractToken();

            MethodInfoField = new FieldDefinition(
                $"Il2CppMethodInfo_{Token.ToString()}_{Original.Name}",
                FieldAttributes.Private | FieldAttributes.Static | FieldAttributes.InitOnly,
                Imports.Structs.Il2CppMethodPointer
            );
        }

        public void Setup()
        {
            DeclaringType.Definition.Fields.Add(MethodInfoField);

            var signature = (MethodSignature?)Original.Signature?.ImportWith(DeclaringType.Assembly.Importer);
            Definition = new MethodDefinition(Original.Name, Original.Attributes, signature);

            foreach (var originalParameter in Original.ParameterDefinitions)
            {
                Definition.ParameterDefinitions.Add(new ParameterDefinition(originalParameter.Sequence, originalParameter.Name, originalParameter.Attributes));
            }

            DeclaringType.Definition.Methods.Add(Definition);

            var body = Definition.CilMethodBody = new CilMethodBody(Definition);
            GenerateBody(Definition, body);
        }

        private void GenerateBody(MethodDefinition definition, CilMethodBody body)
        {
            var instructions = body.Instructions;

            CilLocalVariable? paramsLocal = null;
            var parameters = definition.Parameters;
            if (parameters.Count != 0)
            {
                paramsLocal = new CilLocalVariable(Imports.Structs.Il2CppObjectPointerPointer);
                body.LocalVariables.Add(paramsLocal);

                // Il2CppObject** @params = stackalloc Il2CppObject*[{parameters.Count}];
                instructions.Add(CilOpCodes.Ldc_I4, parameters.Count);
                instructions.Add(CilOpCodes.Conv_U);
                instructions.Add(CilOpCodes.Sizeof, Imports.Structs.Il2CppObjectPointer.ToTypeDefOrRef());
                instructions.Add(CilOpCodes.Mul_Ovf_Un);
                instructions.Add(CilOpCodes.Localloc);
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

                        instructions.Add(CilOpCodes.Sizeof, Imports.Structs.Il2CppObjectPointer.ToTypeDefOrRef());

                        if (i > 1)
                        {
                            instructions.Add(CilOpCodes.Mul);
                        }

                        instructions.Add(CilOpCodes.Add);
                    }

                    if (parameter.ParameterType.IsValueType)
                    {
                        // Il2CppObject.FakeBox(&{parameter});
                        instructions.Add(CilOpCodes.Ldarga, parameter);
                        instructions.Add(CilOpCodes.Conv_U);
                        instructions.Add(CilOpCodes.Call, Imports.Structs.Il2CppObjectFakeBox);
                    }
                    else
                    {
                        // {parameter} != null ? {parameter}.Pointer : null;
                        // TODO use ilhelpers here?
                        var loadPointerLabel = new CilInstructionLabel();
                        var callLabel = new CilInstructionLabel();
                        instructions.Add(CilOpCodes.Ldarg, parameter);
                        instructions.Add(CilOpCodes.Brtrue, loadPointerLabel);
                        instructions.Add(CilOpCodes.Ldc_I4, 0);
                        instructions.Add(CilOpCodes.Conv_U);
                        instructions.Add(CilOpCodes.Br, callLabel);
                        loadPointerLabel.Instruction = instructions.Add(CilOpCodes.Ldarg, parameter);
                        callLabel.Instruction = instructions.Add(CilOpCodes.Call, Imports.Il2CppObjectBase.GetPointer);
                    }

                    instructions.Add(CilOpCodes.Stind_I);
                }
            }

            instructions.Add(CilOpCodes.Ldsfld, MethodInfoField);

            if (!definition.IsStatic)
            {
                // this.Pointer
                instructions.Add(CilOpCodes.Ldarg_0);
                instructions.Add(CilOpCodes.Call, Imports.Il2CppObjectBase.GetPointer);
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
                // (Il2CppObject**)null
                instructions.Add(CilOpCodes.Ldc_I4_0);
                instructions.Add(CilOpCodes.Conv_U);
            }

            // Il2CppMethodInfo_{token}->Invoke(^, ^)
            instructions.Add(CilOpCodes.Call, Imports.Structs.Il2CppMethodInvoke);

            var returnType = definition.Signature!.ReturnType;
            if (returnType.ElementType == ElementType.Void)
            {
                // ;
                instructions.Add(CilOpCodes.Pop);
            }
            else if (returnType.IsValueType)
            {
                // return ^->Unbox<int>();
                instructions.Add(CilOpCodes.Call, Imports.Structs.Il2CppObjectUnbox(returnType));
            }
            else
            {
                // return Il2CppObjectPool.Get<{returnType}>(^);
                // TODO
            }

            instructions.Add(CilOpCodes.Ret);

            body.Instructions.CalculateOffsets();
            body.VerifyLabels(false);
            body.MaxStack = body.ComputeMaxStack(false);
        }
    }
}
