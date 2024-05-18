using AsmResolver;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.DotNet.Signatures;
using AsmResolver.DotNet.Signatures.Types;
using AsmResolver.PE.DotNet.Cil;
using AsmResolver.PE.DotNet.Metadata.Tables.Rows;

namespace Il2CppInterop.Generator.Extensions;

internal static class AsmResolverExtensions
{
    public static AssemblyReference CreateAssemblyReference(this ModuleDefinition module, Utf8String? name, Version version)
    {
        var assemblyReference = new AssemblyReference(name, version);
        module.AssemblyReferences.Add(assemblyReference);
        return assemblyReference;
    }

    public static TypeReference CreateTypeReference(this IResolutionScope scope, ModuleDefinition? module, string? ns, string name)
    {
        return new TypeReference(module, scope, ns, name);
    }

    public static PointerTypeSignature MakePointerType(this ITypeDefOrRef type, bool isValueType)
    {
        return new PointerTypeSignature(type.ToTypeSignature(isValueType));
    }

    public static bool IsStaticConstructor(this MethodDefinition method)
    {
        return method.IsSpecialName && method.IsRuntimeSpecialName && method.Name.Equals(".cctor"u8);
    }

    private static readonly Utf8String _implicitConversionName = "op_Implicit"u8.ToUtf8String();

    public static MethodDefinition CreateImplicitConversion(TypeSignature intoType, TypeSignature fromType)
    {
        var method = new MethodDefinition(
            _implicitConversionName,
            MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static | MethodAttributes.SpecialName,
            MethodSignature.CreateStatic(intoType, fromType)
        );

        method.Parameters.Single().GetOrCreateDefinition().Name = "value";

        return method;
    }

    public static void AddEmptyBody(this MethodDefinition method)
    {
        method.CilMethodBody = new CilMethodBody(method);
        method.CilMethodBody.Instructions.Add(new CilInstruction(0, CilOpCodes.Ret));
    }

    public static bool IsPrimitive(this ElementType elementType)
    {
        switch (elementType)
        {
            case ElementType.Void:
            case ElementType.Boolean:
            case ElementType.Char:
            case ElementType.I1 or ElementType.I2 or ElementType.I4 or ElementType.I8:
            case ElementType.U1 or ElementType.U2 or ElementType.U4 or ElementType.U8:
            case ElementType.R4 or ElementType.R8:
            case ElementType.I or ElementType.U:
                return true;

            default:
                return false;
        }
    }

    public static bool IsPointerLike(this ITypeDescriptor typeSignature)
    {
        if (typeSignature is TypeSpecification typeSpecification) return typeSpecification.Signature!.IsPointerLike();
        return typeSignature is PointerTypeSignature or ByReferenceTypeSignature;
    }

    public static bool IsValueTypeLike(this ITypeDescriptor typeSignature)
    {
        if (typeSignature is TypeSpecification typeSpecification) return typeSpecification.Signature!.IsValueTypeLike();
        return typeSignature.IsValueType || typeSignature.IsPointerLike();
    }
}
