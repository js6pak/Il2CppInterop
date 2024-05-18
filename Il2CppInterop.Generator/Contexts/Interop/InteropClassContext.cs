using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.DotNet.Signatures.Types;
using AsmResolver.PE.DotNet.Cil;
using Il2CppInterop.Generator.Extensions;

namespace Il2CppInterop.Generator.Contexts.Interop;

internal class InteropClassContext : InteropTypeContext
{
    public List<InteropMethodContext> Methods { get; } = new();

    public InteropClassContext(InteropAssemblyContext assembly, TypeDefinition original, InteropTypeContext? declaringType = null) : base(assembly, original, declaringType)
    {
        foreach (var originalMethod in Original.Methods)
        {
            if (originalMethod.IsStaticConstructor()) continue;

            var interopMethod = new InteropMethodContext(this, originalMethod);
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
        var methodDefinition = Definition.GetOrCreateStaticConstructor();

        var body = methodDefinition.CilMethodBody = new CilMethodBody(methodDefinition);
        var instructions = body.Instructions;

        var klassLocal = new CilLocalVariable(Imports.Structs.Il2CppClass.Pointer);
        body.LocalVariables.Add(klassLocal);

        if (DeclaringType != null)
        {
            // Il2CppRuntime.GetNestedClassFromName(Il2CppClassPointerStore<{DeclaringType}>.Pointer, "{Name}")
            instructions.Add(CilOpCodes.Call, Imports.Il2CppClassPointerStore.GenericGet(DeclaringType.Definition.ToTypeSignature()));
            instructions.Add(CilOpCodes.Ldstr, Original.Name);
            instructions.Add(CilOpCodes.Call, Imports.Il2CppRuntime.GetNestedClassFromName);
        }
        else
        {
            // Il2CppRuntime.GetClassFromName({Module.Name}, {Namespace}, {Name})
            instructions.Add(CilOpCodes.Ldstr, Assembly.OriginalModule.Name);
            instructions.AddLdStrOrNull(Original.Namespace);
            instructions.Add(CilOpCodes.Ldstr, Original.Name);
            instructions.Add(CilOpCodes.Call, Imports.Il2CppRuntime.GetClassFromName);
        }

        if (Original.GenericParameters.Any())
        {
            // LightReflection.MakeGenericClass(^, Il2CppClassPointerStore<T0>.Pointer...)
            instructions.Add(CilOpCodes.Ldc_I4, Original.GenericParameters.Count);
            instructions.Add(CilOpCodes.Newarr, Imports.Structs.Il2CppClass.Pointer.ToTypeDefOrRef());

            for (var i = 0; i < Original.GenericParameters.Count; i++)
            {
                instructions.Add(CilOpCodes.Dup);
                instructions.Add(CilOpCodes.Ldc_I4, i);
                instructions.Add(CilOpCodes.Call, Imports.Il2CppClassPointerStore.GenericGet(new GenericParameterSignature(GenericParameterType.Type, i)));
                instructions.Add(CilOpCodes.Stelem_I);
            }

            instructions.Add(CilOpCodes.Call, Imports.LightReflection.MakeGenericClass);
        }

        // Il2CppClass* klass = ^;
        instructions.Add(CilOpCodes.Stloc, klassLocal);

        // Il2CppClassPointerStore<{this}>.Pointer = klass;
        instructions.Add(CilOpCodes.Ldloc, klassLocal);
        instructions.Add(CilOpCodes.Call, Imports.Il2CppClassPointerStore.GenericSet(Definition.ToTypeSignature()));

        // InteropTypeStore.Set({this}, klass);
        instructions.Add(CilOpCodes.Ldloc, klassLocal);
        instructions.Add(CilOpCodes.Ldtoken, Definition);
        instructions.Add(CilOpCodes.Call, Imports.GetTypeFromHandle);
        instructions.Add(CilOpCodes.Call, Imports.InteropTypeStore.Set);

        // Il2CppRuntime.ClassInit(klass);
        instructions.Add(CilOpCodes.Ldloc, klassLocal);
        instructions.Add(CilOpCodes.Call, Imports.Il2CppRuntime.ClassInit);

        ConfigureStaticConstructorBody(body, klassLocal);

        instructions.Add(CilOpCodes.Ret);

        instructions.OptimizeMacros();
    }

    protected virtual void ConfigureStaticConstructorBody(CilMethodBody body, CilLocalVariable klassLocal)
    {
        foreach (var method in Methods)
        {
            // Il2CppMethodInfo_{token} = klass->GetMethodByToken({token});
            body.Instructions.Add(CilOpCodes.Ldloc, klassLocal);
            body.Instructions.Add(CilOpCodes.Ldc_I4, (int)method.Token);
            body.Instructions.Add(CilOpCodes.Call, Imports.Structs.Il2CppClass.GetMethodByToken);
            body.Instructions.Add(CilOpCodes.Stsfld, method.MethodInfoField);
        }
    }
}
